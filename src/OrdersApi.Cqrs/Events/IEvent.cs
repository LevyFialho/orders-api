using System;
using OrdersApi.Cqrs.Messages;
using MediatR;

namespace OrdersApi.Cqrs.Events
{
    /// <summary>
    /// Interface for all events
    /// </summary>
    public interface IEvent: INotification
    {
        /// <summary>
        /// Target version of the Aggregate this event will be applied against
        /// </summary>
        short TargetVersion { get; set; }

        /// <summary>
        /// Saga identifier
        /// </summary>
        string SagaProcessKey { get; set; }

        /// <summary>
        /// Event ID
        /// </summary>
        string EventKey { get;  }

        /// <summary>
        /// The aggregateID of the aggregate
        /// </summary>
        string AggregateKey { get; set; }

        /// <summary>
        /// Requester ID
        /// </summary>
        string ApplicationKey { get; set; }  

        /// <summary>
        /// Correlation ID  
        /// </summary>
        string CorrelationKey { get; }

        /// <summary>
        /// This is used to timestamp the event when it get's committed
        /// </summary>
        DateTime EventCommittedTimestamp { get; set; }

        /// <summary>
        /// This is used to handle versioning of events over time when refactoring or feature additions are done
        /// </summary>
        int ClassVersion { get; set; }
    }
}
