using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Cqrs.Events;

namespace OrdersApi.Domain.Events.ClientApplication
{
    public class ClientApplicationActivated : Event
    {
        private static int _currrentTypeVersion = 1; 
         
         

        public ClientApplicationActivated(string aggregateKey, string correlationKey, string applicationKey, short version, string sagaProcessKey)
            : base(aggregateKey, correlationKey, applicationKey, version, _currrentTypeVersion, sagaProcessKey)
        { 
        }

    }
}
