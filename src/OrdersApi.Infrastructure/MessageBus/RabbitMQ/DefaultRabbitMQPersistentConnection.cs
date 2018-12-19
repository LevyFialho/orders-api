using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;

namespace OrdersApi.Infrastructure.MessageBus.RabbitMQ
{
    public class DefaultRabbitMqPersistentConnection : IEventBusRabbitMQPersistentConnection, 
                                                       ICommandBusRabbitMQPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<DefaultRabbitMqPersistentConnection> _logger;
        private readonly int _retryCount;

        IConnection _connection;
        IConnection _commandBusConnection;
        bool _disposed;

        readonly object _syncRoot = new object();

        public DefaultRabbitMqPersistentConnection(IConnectionFactory connectionFactory, 
                                                   ILogger<DefaultRabbitMqPersistentConnection> logger, 
                                                   int retryCount = 5)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryCount = retryCount;
        }

        public bool IsConnectedToEventBus => (_connection != null && _connection.IsOpen) && !_disposed;

        public IModel CreateEventBusModel()
        {
            if (!IsConnectedToEventBus)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return _connection.CreateModel();
        }

        public bool IsConnectedToCommandBus => (_commandBusConnection != null && _commandBusConnection.IsOpen) && !_disposed;

        public IModel CreateCommandBusModel()
        {
            if (!IsConnectedToCommandBus)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return _commandBusConnection.CreateModel();
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
                try
                {
                    _connection.Dispose();
                }
                catch (IOException ex)
                {
                    _logger.LogCritical(ex.ToString());
                }
            }

            _disposed = true;
        }

        #region EventBus

        public bool TryConnectToEventBus()
        {
            _logger.LogInformation("RabbitMQ Client is trying to connect to event bus");

            lock (_syncRoot)
            {
                var policy = Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning(ex.ToString());
                    }
                );

                policy.Execute((Action)(() =>
                {
                    _connection = this._connectionFactory
                          .CreateConnection();
                }));

                if (IsConnectedToEventBus)
                {
                    _connection.ConnectionShutdown += OnEventBusConnectionShutdown;
                    _connection.CallbackException += OnEventBusCallbackException;
                    _connection.ConnectionBlocked += OnEventBusConnectionBlocked;

                    _logger.LogInformation($"RabbitMQ persistent connection acquired a connection {_connection.Endpoint.HostName} and is subscribed to failure events");

                    return true;
                }
                else
                {
                    _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                    return false;
                }
            }
        }

        private void OnEventBusConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ event bus connection is shutdown. Trying to re-connect...");

            TryConnectToEventBus();
        }

        void OnEventBusCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ event bus connection throw exception. Trying to re-connect...");

            TryConnectToEventBus();
        }

        void OnEventBusConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ event bus connection is on shutdown. Trying to re-connect...");

            TryConnectToEventBus();
        }

        #endregion

        #region CommandBus

        public bool TryConnectToCommandBus()
        {
            _logger.LogInformation("RabbitMQ Client is trying to connect to command bus queue");

            lock (_syncRoot)
            {
                var policy = Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning(ex.ToString());
                    }
                );

                policy.Execute((Action)(() =>
                {
                    _commandBusConnection = this._connectionFactory
                          .CreateConnection();
                }));

                if (IsConnectedToCommandBus)
                {
                    _commandBusConnection.ConnectionShutdown += OnCommandBusConnectionShutdown;
                    _commandBusConnection.CallbackException += OnCommandBusCallbackException;
                    _commandBusConnection.ConnectionBlocked += OnCommandBusConnectionBlocked;

                    _logger.LogInformation($"RabbitMQ persistent connection acquired a connection {_commandBusConnection.Endpoint.HostName} and is subscribed to failure commands");

                    return true;
                }
                else
                {
                    _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                    return false;
                }
            }
        }

        private void OnCommandBusConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ command bus connection is shutdown. Trying to re-connect...");

            TryConnectToCommandBus();
        }

        void OnCommandBusCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ  command bus connection threw exception. Trying to re-connect...");

            TryConnectToCommandBus();
        }

        void OnCommandBusConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ  command bus connection is on shutdown. Trying to re-connect...");

            TryConnectToCommandBus();
        }

        #endregion
    }
}
