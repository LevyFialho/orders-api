using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using OrdersApi.Cqrs.Events;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using OrdersApi.Infrastructure.MessageBus.EventBus;
using OrdersApi.Infrastructure.Settings;
using MediatR;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace OrdersApi.Infrastructure.MessageBus.ServiceBus
{
    public class AzureEventBus : IEventBus, IDisposable
    {
        private readonly IServiceBusPersisterConnection _serviceBusPersisterConnection;
        private readonly ILogger<AzureEventBus> _logger;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly SubscriptionClient _subscriptionClient;
        private readonly ILifetimeScope _autofac;
        private readonly int _maxConcurrentCalls;
        private readonly string AUTOFAC_SCOPE_NAME = "event_bus_scope";
        private IEnumerable<RuleDescription> _rules;
        private bool _disposed;

        public AzureEventBus(IServiceBusPersisterConnection serviceBusPersisterConnection, IEventBusSubscriptionsManager subsManager, 
            MessageBrokerSettings settings, ILifetimeScope autofac, ILogger<AzureEventBus> logger)
        {
            _logger = logger;
            _maxConcurrentCalls = settings.EventBusMaxConcurrentCalls;
            _serviceBusPersisterConnection = serviceBusPersisterConnection; 
            _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();

            _subscriptionClient = new SubscriptionClient(serviceBusPersisterConnection.EventBusConnectionStringBuilder,
                settings.EventBusSubscriptionClientName);
            _subscriptionClient.PrefetchCount = settings.EventBusPrefetchCount;
           
            _autofac = autofac;
            LoadRules().Wait(CancellationToken.None);
            RemoveRules(settings.ClearSubscriptionOnStartup).Wait(CancellationToken.None);
            RegisterSubscriptionClientMessageHandler();
        }

        public void Publish(IEvent @event)
        {
            var eventName = @event.GetType().Name;
            var jsonMessage = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(jsonMessage);
            
            var message = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = body,
                Label = eventName, 
                ContentType = "application/json",
                CorrelationId = @event.EventKey
            };
            var topicClient = _serviceBusPersisterConnection.CreateModel();

            topicClient.SendAsync(message)
                .GetAwaiter()
                .GetResult();
        }

        public void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationHandler
        {
            _subsManager.AddDynamicSubscription<TH>(eventName);
        }

        public void Subscribe<T, TH>()
            where T : IEvent
            where TH : IEventHandler<T>
        {
            var eventName = typeof(T).Name;

            var containsKey = _subsManager.HasSubscriptionsForEvent<T>();
            if (!containsKey)
            {
                _subsManager.AddSubscription<T, TH>();
                if (_rules.All(x => x.Name != eventName))
                {
                    try
                    {
                        _subscriptionClient.AddRuleAsync(new RuleDescription
                        {
                            Filter = new CorrelationFilter { Label = eventName },
                            Name = eventName
                        }).GetAwaiter().GetResult();
                    }
                    catch (ServiceBusException)
                    {
                        _logger.LogInformation($"The messaging entity {eventName} already exists.");
                    }
                }
            }

        }

        public void Unsubscribe<T, TH>()
            where T : IEvent
            where TH : IEventHandler<T>
        {
            var eventName = typeof(T).Name;

            try
            {
                _subscriptionClient
                 .RemoveRuleAsync(eventName)
                 .GetAwaiter()
                 .GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger.LogInformation($"The messaging entity {eventName} Could not be found.");
            }

            _subsManager.RemoveSubscription<T, TH>();
        }

        public void UnsubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationHandler
        {
            _subsManager.RemoveDynamicSubscription<TH>(eventName);
        }
         

        [ExcludeFromCodeCoverage]
        public void Dispose()
        {

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [ExcludeFromCodeCoverage]
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) { return; }

            if (disposing)
            {
                _subsManager?.Clear();
                _autofac?.Dispose();
            }

            _disposed = true;
        }

        private async Task LoadRules()
        {
            _rules = await _subscriptionClient.GetRulesAsync();
        }

        private void RegisterSubscriptionClientMessageHandler()
        { 
            _subscriptionClient.RegisterMessageHandler(
                async (message, token) =>
                {
                    var eventName = $"{message.Label}";
                    var messageData = Encoding.UTF8.GetString(message.Body);
                    await ProcessEvent(eventName, messageData);

                    // Complete the message so that it is not received again.
                    await _subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                },
               new MessageHandlerOptions(ExceptionReceivedHandler)
               {
                   MaxConcurrentCalls = _maxConcurrentCalls,
                   AutoComplete = false,  
               });
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var builder = new StringBuilder();
            builder.Append($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            builder.Append("Exception context for troubleshooting:");
            builder.Append($"- Endpoint: {context.Endpoint}");
            builder.Append($"- Entity Path: {context.EntityPath}");
            builder.Append($"- Executing Action: {context.Action}");
            _logger.LogError(builder.ToString());
            return Task.CompletedTask;
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            try
            {
                if (_subsManager.HasSubscriptionsForEvent(eventName))
                {
                    using (var scope = _autofac.BeginLifetimeScope(AUTOFAC_SCOPE_NAME))
                    {
                        var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                        foreach (var subscription in subscriptions)
                        {
                            if (subscription.IsDynamic)
                            {
                                var handler = scope.ResolveOptional(subscription.HandlerType) as IDynamicIntegrationHandler;
                                dynamic eventData = JObject.Parse(message);
                                await handler.Handle(eventData);
                            }
                            else
                            {

                                var eventType = _subsManager.GetEventTypeByName(eventName);
                                var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                                var handler = scope.ResolveOptional(subscription.HandlerType);

                                if (handler == null)
                                {
                                    throw new EntryPointNotFoundException($"Handler {subscription.HandlerType} not registered to event type {eventType}. ");
                                }

                                var concreteType = typeof(INotificationHandler<>).MakeGenericType(eventType);
                                var method = concreteType.GetMethod("Handle");
                                await (Task)method.Invoke(handler, new object[] { integrationEvent, CancellationToken.None });
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, e.ToString());
                throw;
            }
        }

        private async Task RemoveRules(bool clearSubscriptions)
        {
            foreach (var r in _rules.Where(x => clearSubscriptions || x.Name == RuleDescription.DefaultRuleName))
            {
                try
                {
                    await _subscriptionClient.RemoveRuleAsync(r.Name);
                }
                catch (MessagingEntityNotFoundException)
                {
                    _logger.LogInformation($"The messaging entity { RuleDescription.DefaultRuleName } Could not be found.");
                }
            }
        }
    }
}
