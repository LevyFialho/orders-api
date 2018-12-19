using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.Infrastructure.Settings
{
    [ExcludeFromCodeCoverage]
    public class MessageBrokerSettings
    {
        public const string SectionName = "MessageBrokerSettings";

        public string EventBusSubscriptionClientName { get; set; }

        public MessageBusType MessageBusType { get; set; }
         
        public int RetryCount { get; set; }

        public int EventBusMaxConcurrentCalls { get; set; }

        public int EventBusPrefetchCount { get; set; }

        public bool ClearSubscriptionOnStartup { get; set; }

        public string RabbitMqQueueName { get; set; }

        public string EventBusConnection { get; set; }

        public string RabbitMqConnection { get; set; }

        public string RabbitMqUserName { get; set; }

        public string RabbitMqPassword { get; set; }

        public int MinimumRetryBackoffSeconds { get; set; }

        public int MaximumRetryBackoffSeconds { get; set; }

        public string RabbitMqBrokerName { get; set; }

        public string ScopeName { get; set; }

        public int CommandBusMaxConcurrentCalls { get; set; }

        public int CommandBusPrefetchCount { get; set; }

        public string CommandBusConnection { get; set; }

        public string RabbitMqCommandBusConnection { get; set; }

        public string RabbitMqCommandBusBrokerName { get; set; }

        public string RabbitMqCommandBusQueueName { get; set; }

        public string RabbitMqCommandBusUserName { get; set; }

        public string RabbitMqCommandBusPassword { get; set; }

        public bool UseDefaultCommandScheduler { get; set; }
    }
    
    public enum MessageBusType
    {
        Inmemory = 0,
        RabbitMq = 1,
        Azzure = 2
    }
}
