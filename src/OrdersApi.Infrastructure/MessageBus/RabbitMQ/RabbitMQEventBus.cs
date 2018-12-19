using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks; 
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly; 
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Autofac;
using OrdersApi.Cqrs.Events;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using IModel = RabbitMQ.Client.IModel;
using OrdersApi.Infrastructure.MessageBus.EventBus;
using OrdersApi.Infrastructure.Settings;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OrdersApi.Infrastructure.MessageBus.RabbitMQ
{
    public class RabbitMQEventBus : IEventBus, IDisposable
    {
        private readonly string _brokerName;

        private readonly IEventBusRabbitMQPersistentConnection _persistentConnection;
        private readonly ILogger<RabbitMQEventBus> _logger;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly ILifetimeScope _autofac;
        private readonly string _autofacScopeName;
        private readonly int _retryCount;
        private bool _disposed;
        private IModel _consumerChannel;
        private string _queueName;

        public RabbitMQEventBus(IEventBusRabbitMQPersistentConnection persistentConnection, 
                                ILifetimeScope autofac, 
                                IEventBusSubscriptionsManager subsManager, 
                                ILogger<RabbitMQEventBus> logger, 
                                IOptions<MessageBrokerSettings> options)
        {
            var settings = options.Value;

            _brokerName = settings.RabbitMqBrokerName;
            _logger = logger;
            _autofacScopeName = settings.ScopeName;
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
            _queueName = string.IsNullOrWhiteSpace(settings.RabbitMqQueueName) ? default(string) : settings.RabbitMqQueueName;
            _consumerChannel = CreateConsumerChannel();
            _autofac = autofac;
            _retryCount = settings.RetryCount;
            _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;

        }

        private void SubsManager_OnEventRemoved(object sender, string eventName)
        {
            if (!_persistentConnection.IsConnectedToEventBus)
            {
                _persistentConnection.TryConnectToEventBus();
            }

            using (var channel = _persistentConnection.CreateEventBusModel())
            {
                channel.QueueUnbind(queue: _queueName,
                    exchange: _brokerName,
                    routingKey: eventName);

                if (!_subsManager.IsEmpty) return;
                _queueName = string.Empty;
                _consumerChannel.Close();
            }
        }

        public void Publish(IEvent @event)
        {
            if (!_persistentConnection.IsConnectedToEventBus)
            {
                _persistentConnection.TryConnectToEventBus();
            }

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, ex.ToString());
                });

            using (var channel = _persistentConnection.CreateEventBusModel())
            {
                var eventName = @event.GetType()
                    .Name;

                channel.ExchangeDeclare(exchange: _brokerName,
                                    type: "direct");

                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);

                policy.Execute(() =>
                {
                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = 2; // persistent

                    channel.BasicPublish(exchange: _brokerName,
                                     routingKey: eventName,
                                     mandatory: true,
                                     basicProperties: properties,
                                     body: body);
                });
            }
        }

        public void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationHandler
        {
            DoInternalSubscription(eventName);
            _subsManager.AddDynamicSubscription<TH>(eventName);
        }

        public void Subscribe<T, TH>()
            where T : IEvent
            where TH : IEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();
            DoInternalSubscription(eventName);
            _subsManager.AddSubscription<T, TH>();
        }

        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnectedToEventBus)
                {
                    _persistentConnection.TryConnectToEventBus();
                }

                using (var channel = _persistentConnection.CreateEventBusModel())
                {
                    channel.QueueBind(queue: _queueName,
                                      exchange: _brokerName,
                                      routingKey: eventName);
                }
            }
        }

        public void Unsubscribe<T, TH>()
            where TH : IEventHandler<T>
            where T : IEvent
        {
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
                if (_consumerChannel != null)
                {
                    _consumerChannel.Dispose();
                }

                _subsManager.Clear();
            }

            _disposed = true;
        }

        private IModel CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnectedToEventBus)
            {
                _persistentConnection.TryConnectToEventBus();
            }

            var channel = _persistentConnection.CreateEventBusModel();

            channel.ExchangeDeclare(exchange: _brokerName,
                                 type: "direct");

            channel.QueueDeclare(queue: _queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);


            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var eventName = ea.RoutingKey;
                var message = Encoding.UTF8.GetString(ea.Body);

                await ProcessEvent(eventName, message);

                channel.BasicAck(ea.DeliveryTag, multiple: false);
            };

            channel.BasicConsume(queue: _queueName,
                                 autoAck: false,
                                 consumer: consumer);

            channel.CallbackException += (sender, ea) =>
            {
                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
            };

            return channel;
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            try
            {

                if (_subsManager.HasSubscriptionsForEvent(eventName))
                {
                    using (var scope = _autofac.BeginLifetimeScope(_autofacScopeName))
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
    }
}
