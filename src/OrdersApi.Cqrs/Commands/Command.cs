using System;
using FluentValidation.Results;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;

namespace OrdersApi.Cqrs.Commands
{
    public abstract class Command : ICommand
    { 
        public DateTime Timestamp { get; private set; }

        public string CommandKey { get; set; }

        public string CorrelationKey { get; set; }

        public string SagaProcessKey { get; set; }

        public string AggregateKey { get; set; }

        public string ApplicationKey { get; set; } 

        public virtual ValidationResult ValidationResult { get; set; }

        public abstract bool IsValid();

        protected Command(string aggregateKey, string correlationKey, string applicationKey, string sagaProcessKey)
        {
            CorrelationKey = correlationKey;
            CommandKey = IdentityGenerator.NewSequentialIdentity();
            AggregateKey = aggregateKey; 
            ApplicationKey = applicationKey;
            SagaProcessKey = sagaProcessKey;
            Timestamp = DateTime.UtcNow; 
        }
    }
}
