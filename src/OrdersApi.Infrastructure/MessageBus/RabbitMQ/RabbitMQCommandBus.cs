using System;
using System.Collections.Generic;
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
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using IModel = RabbitMQ.Client.IModel;
using OrdersApi.Infrastructure.MessageBus.CommandBus;
using OrdersApi.Infrastructure.Settings;
using MediatR;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
#pragma warning disable CS1998
#pragma warning disable S3776

namespace OrdersApi.Infrastructure.MessageBus.RabbitMQ
{
    public class RabbitMQCommandBus : ICommandBus, IDisposable
    {
        private readonly string _brokerName;

        private readonly ICommandBusRabbitMQPersistentConnection _persistentConnection;
        private readonly ILogger<RabbitMQCommandBus> _logger;
        private readonly ICommandBusSubscriptionsManager _subsManager;
        private readonly ILifetimeScope _autofac;
        private readonly string _autofacScopeName;
        private readonly int _retryCount;
        private bool _disposed;
        private IModel _consumerChannel;
        private string _queueName;

        public RabbitMQCommandBus(ICommandBusRabbitMQPersistentConnection persistentConnection,
                                  ILifetimeScope autofac, 
                                  ICommandBusSubscriptionsManager subsManager, 
                                  ILogger<RabbitMQCommandBus> logger, 
                                  IOptions<MessageBrokerSettings> options)
        {
            var settings = options.Value;

            _brokerName = settings.RabbitMqCommandBusBrokerName;
            _logger = logger;
            _autofacScopeName = settings.ScopeName;
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _subsManager = subsManager ?? new InMemoryCommandBusSubscriptionsManager();
            _queueName = settings.RabbitMqCommandBusQueueName;
            _consumerChannel = CreateConsumerChannel();
            _autofac = autofac;
            _retryCount = settings.RetryCount;
            _subsManager.OnCommandRemoved += SubsManager_OnCommandRemoved;

        }

        private void SubsManager_OnCommandRemoved(object sender, string commandName)
        {
            if (!_persistentConnection.IsConnectedToCommandBus)
            {
                _persistentConnection.TryConnectToCommandBus();
            }

            using (var channel = _persistentConnection.CreateCommandBusModel())
            {
                channel.QueueUnbind(queue: _queueName,
                    exchange: _brokerName,
                    routingKey: commandName);

                if (!_subsManager.IsEmpty) return;
                _queueName = string.Empty;
                _consumerChannel.Close();
            }
        }

        public virtual async Task Publish(ICommand @command, DateTime? scheduledEnqueueTimeUtc = null)
        {
            int milliseconds = -1;
            if (scheduledEnqueueTimeUtc != null)
                milliseconds = (scheduledEnqueueTimeUtc.Value - DateTime.UtcNow).Milliseconds;
            if (milliseconds < 0) milliseconds = 0;

            if (!_persistentConnection.IsConnectedToCommandBus)
            {
                _persistentConnection.TryConnectToCommandBus();
            }

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, ex.ToString());
                });

            using (var channel = _persistentConnection.CreateCommandBusModel())
            {
                var commandName = @command.GetType().Name;
                channel.ExchangeDeclare(exchange: _brokerName,
                                    type: "x-delayed-message",
                                    durable: true,
                                    autoDelete: false,
                                    arguments: new Dictionary<string, object> { { "x-delayed-type", "direct" } });

                var message = JsonConvert.SerializeObject(@command);
                var body = Encoding.UTF8.GetBytes(message);

                policy.Execute(() =>
                {
                    var properties = channel.CreateBasicProperties();
                    var headers = new Dictionary<string, object>();
                    headers.Add("x-delay", milliseconds.ToString());
                    properties.Headers = headers;
                    properties.DeliveryMode = 2; // persistent
                    channel.BasicPublish(exchange: _brokerName,
                                     routingKey: commandName,
                                     mandatory: true,
                                     basicProperties: properties,
                                     body: body);
                });
            }
        }
         

        public void Subscribe<T, TH>()
            where T : ICommand
            where TH : IRequestHandler<T>
        {
            var commandName = _subsManager.GetCommandKey<T>();
            DoInternalSubscription(commandName);
            _subsManager.AddSubscription<T, TH>();
        }

        private void DoInternalSubscription(string commandName)
        {
            var containsKey = _subsManager.HasSubscriptionsForCommand(commandName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnectedToCommandBus)
                {
                    _persistentConnection.TryConnectToCommandBus();
                }

                using (var channel = _persistentConnection.CreateCommandBusModel())
                {
                    channel.QueueBind(queue: _queueName,
                                      exchange: _brokerName,
                                      routingKey: commandName);
                }
            }
        }

        public void Unsubscribe<T, TH>()
            where TH : IRequestHandler<T>
            where T : ICommand
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
                _consumerChannel?.Dispose();

                _subsManager.Clear();
            }

            _disposed = true;
        }

        private IModel CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnectedToCommandBus)
            {
                _persistentConnection.TryConnectToCommandBus();
            }

            var channel = _persistentConnection.CreateCommandBusModel();

            channel.ExchangeDeclare(exchange: _brokerName,
                                    type: "x-delayed-message",
                                    durable: true,
                                    autoDelete: false,
                                    arguments: new Dictionary<string, object> { { "x-delayed-type", "direct" } });

            channel.QueueDeclare(queue: _queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);


            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var commandName = ea.RoutingKey;
                var message = Encoding.UTF8.GetString(ea.Body);

                await ProcessCommand(commandName, message);

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

        private async Task ProcessCommand(string commandName, string message)
        {
            try
            {

                if (_subsManager.HasSubscriptionsForCommand(commandName))
                {
                    using (var scope = _autofac.BeginLifetimeScope(_autofacScopeName))
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
