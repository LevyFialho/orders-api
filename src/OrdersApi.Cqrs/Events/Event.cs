using System;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using MediatR;

namespace OrdersApi.Cqrs.Events
{ 
    public class Event : IEvent
    { 
        public short TargetVersion { get; set; }

        public string EventKey { get; set;  }

        public string SagaProcessKey { get; set; }

        public string CorrelationKey { get; set; }

        public string AggregateKey { get; set; }

        public string ApplicationKey { get; set; }

        public DateTime EventCommittedTimestamp { get; set; }

        public int ClassVersion { get; set; }
         
        public Event()
        {
        }
  
        public Event(string aggregateKey, string correlationKey, string applicationKey,  short targetVersion, int eventClassVersion, string sagaProcessKey):base()
        {
            AggregateKey = aggregateKey;
            TargetVersion = targetVersion;
            ClassVersion = eventClassVersion;
            CorrelationKey = correlationKey;
            ApplicationKey = applicationKey;
            SagaProcessKey = sagaProcessKey;
            EventKey = IdentityGenerator.NewSequentialIdentity();
        }
    }

}
