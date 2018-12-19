using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
#pragma warning disable S1450
namespace OrdersApi.Infrastructure.MessageBus.ServiceBus
{ 
    public class DefaultServiceBusPersisterConnection : IServiceBusPersisterConnection
    {
        private readonly ILogger<DefaultServiceBusPersisterConnection> _logger;
        private readonly ServiceBusConnectionStringBuilder _eventBusConnectionStringBuilder;
        private readonly ServiceBusConnectionStringBuilder _commandBusConnectionStringBuilder;
        private ITopicClient _topicClient;
        private IQueueClient _queueClient;
        private readonly int _retryCount;
        private readonly int _mimimumRetryBackoffSeconds;
        private readonly int _maximumRetryBackoffSeconds; 
        bool _disposed;

        public DefaultServiceBusPersisterConnection(ServiceBusConnectionStringBuilder eventBusConnectionStringBuilder, 
            ILogger<DefaultServiceBusPersisterConnection> logger, int retryCount, int minimumRetryBackoffSeconds, int maximumRetryBackoffSeconds, ServiceBusConnectionStringBuilder commandBusConnectionStringBuilder)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _eventBusConnectionStringBuilder = eventBusConnectionStringBuilder ??
                                                 throw new ArgumentNullException(nameof(eventBusConnectionStringBuilder));
            _commandBusConnectionStringBuilder = commandBusConnectionStringBuilder ??
                                               throw new ArgumentNullException(nameof(commandBusConnectionStringBuilder));

            _retryCount = retryCount;
            _mimimumRetryBackoffSeconds = minimumRetryBackoffSeconds;
            _maximumRetryBackoffSeconds = maximumRetryBackoffSeconds;
            _topicClient = GetTopicClient();
            _queueClient = GetQueueClient();
        }

        private TopicClient GetTopicClient()
        { 
            return new TopicClient(_eventBusConnectionStringBuilder, new RetryExponential(TimeSpan.FromSeconds(_mimimumRetryBackoffSeconds), TimeSpan.FromSeconds(_maximumRetryBackoffSeconds), _retryCount));
        }

        private QueueClient GetQueueClient()
        {
            return new QueueClient(_commandBusConnectionStringBuilder, ReceiveMode.PeekLock,
                new RetryExponential(TimeSpan.FromSeconds(_mimimumRetryBackoffSeconds),
                    TimeSpan.FromSeconds(_maximumRetryBackoffSeconds), _retryCount));
        }

        public ServiceBusConnectionStringBuilder EventBusConnectionStringBuilder => _eventBusConnectionStringBuilder;
        public ServiceBusConnectionStringBuilder CommandBusConnectionStringBuilder => _commandBusConnectionStringBuilder;

        public ITopicClient CreateModel()
        {
            if (_topicClient.IsClosedOrClosing)
            {
                _topicClient = GetTopicClient();
            }

            return _topicClient;
        }

        public IQueueClient CreateQueueModel()
        {
            if (_queueClient.IsClosedOrClosing)
            {
                _queueClient = GetQueueClient();
            }

            return _queueClient;
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
             

            _disposed = true;
        }
    }
}
