using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Cqrs.Events;

namespace OrdersApi.Domain.Events.ClientApplication
{
    public class ClientApplicationCreationRevoked : Event
    {
        private static int _currrentTypeVersion = 1; 
         
        public string Reason { get; set; }

        public ClientApplicationCreationRevoked(string aggregateKey, string correlationKey, 
            string applicationKey, short version, string reason, string sagaProcessKey)
            : base(aggregateKey, correlationKey, applicationKey, version, _currrentTypeVersion, sagaProcessKey)
        {
            Reason = reason;
        }

    }
}
