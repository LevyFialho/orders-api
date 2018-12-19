using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using OrdersApi.Infrastructure.MessageBus.CommandBus;
using OrdersApi.Infrastructure.Settings;
using MediatR;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
#pragma warning disable S3776

namespace OrdersApi.Infrastructure.MessageBus.ServiceBus
{
    public class AzureCommandBus : ICommandBus, IDisposable
    {
        private readonly IServiceBusPersisterConnection _serviceBusPersisterConnection;
        private readonly ILogger<AzureCommandBus> _logger;
        private readonly ICommandBusSubscriptionsManager _subsManager;
        private readonly IQueueClient _queueClient;
        private readonly ILifetimeScope _autofac;
        private readonly int _maxConcurrentCalls;
        private readonly string AUTOFAC_SCOPE_NAME = "command_bus_scope";
        private bool _disposed;

        public AzureCommandBus(IServiceBusPersisterConnection serviceBusPersisterConnection, ICommandBusSubscriptionsManager subsManager,
            MessageBrokerSettings settings, ILifetimeScope autofac, ILogger<AzureCommandBus> logger)
        {
            _logger = logger;
            _maxConcurrentCalls = settings.CommandBusMaxConcurrentCalls;
            _serviceBusPersisterConnection = serviceBusPersisterConnection;
            _subsManager = subsManager ?? new InMemoryCommandBusSubscriptionsManager();
            _queueClient = serviceBusPersisterConnection.CreateQueueModel();
            _queueClient.PrefetchCount = settings.CommandBusPrefetchCount;
            _autofac = autofac;
            RegisterSubscriptionClientMessageHandler();
        }

        public async Task Publish(ICommand @command, DateTime? scheduledEnqueueTimeUtc = null)
        {
            if (scheduledEnqueueTimeUtc == null)
                scheduledEnqueueTimeUtc = DateTime.UtcNow;

            var commandName = @command.GetType().Name;
            var jsonMessage = JsonConvert.SerializeObject(@command);
            var body = Encoding.UTF8.GetBytes(jsonMessage);

            var message = new Message
            {
                MessageId = @command.CommandKey,
                Body = body,
                Label = commandName,
                ContentType = "application/json",
                CorrelationId = @command.CorrelationKey,
                ScheduledEnqueueTimeUtc = scheduledEnqueueTimeUtc.Value
            };
            var queueClient = _serviceBusPersisterConnection.CreateQueueModel();
            await queueClient.SendAsync(message);
        }

        public void Subscribe<T, TH>()
            where T : ICommand
            where TH : IRequestHandler<T>
        {

            var containsKey = _subsManager.HasSubscriptionsForCommand<T>();
            if (!containsKey)
            {
                _subsManager.AddSubscription<T, TH>();
            }

        }

        public void Unsubscribe<T, TH>()
            where T : ICommand
            where TH : IRequestHandler<T>
        {
            _subsManager.RemoveSubscription<T, TH>();
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

        private void RegisterSubscriptionClientMessageHandler()
        {
            _queueClient.RegisterMessageHandler(
                async (message, token) =>
                {
                    var commandName = $"{message.Label}";
                    var messageData = Encoding.UTF8.GetString(message.Body);
                    await ProcessCommand(commandName, messageData);

                    // Complete the message so that it is not received again.
                    await _queueClient.CompleteAsync(message.SystemProperties.LockToken);
                },
               new MessageHandlerOptions(ExceptionReceivedHandler)
               {
                   MaxConcurrentCalls = _maxConcurrentCalls,
                   AutoComplete = false,

               });
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedCommandArgs)
        {
            var builder = new StringBuilder();
            builder.Append($"Command handler encountered an exception {exceptionReceivedCommandArgs.Exception}.");
            var context = exceptionReceivedCommandArgs.ExceptionReceivedContext;
            builder.Append("Exception context for troubleshooting:");
            builder.Append($"- Endpoint: {context.Endpoint}");
            builder.Append($"- Entity Path: {context.EntityPath}");
            builder.Append($"- Executing Action: {context.Action}");
            _logger.LogError(builder.ToString());
            return Task.CompletedTask;
        }

        private async Task ProcessCommand(string commandName, string message)
        {
            try
            {
                if (_subsManager.HasSubscriptionsForCommand(commandName))
                {
                    using (var scope = _autofac.BeginLifetimeScope(AUTOFAC_SCOPE_NAME))
                    {
                        var subscriptions = _subsManager.GetHandlersForCommand(commandName);
                        foreach (var subscription in subscriptions)
                        {
                            var commandType = _subsManager.GetCommandTypeByName(commandName);
                            var integrationCommand = JsonConvert.DeserializeObject(message, commandType);
                            var handler = scope.ResolveOptional(subscription.HandlerType);
                            var concreteType = typeof(IRequestHandler<,>).MakeGenericType(commandType, typeof(MediatR.Unit));
                            var method = concreteType.GetMethod("Handle");
                            if (handler == null || method == null)
                            {
                                throw new CommandExecutionFailedException($"Handler {subscription.HandlerType} not registered to command type {commandType}. ");
                            }
                            await (Task<Unit>)method.Invoke(handler, new object[] { integrationCommand, CancellationToken.None });
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
