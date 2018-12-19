using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Cqrs.Events;
using OrdersApi.Domain.Model.ClientApplicationAggregate;

namespace OrdersApi.Domain.Events.ClientApplication
{
    public class ProductAccessUpdated : Event
    {
        private static int _currrentTypeVersion = 1;

        public ProductAccess ProductAccess { get; set; }
        
        public ProductAccessUpdated(string aggregateKey, string correlationKey,
            string applicationKey, short version, ProductAccess access, string sagaProcessKey)
            : base(aggregateKey, correlationKey, applicationKey, version, _currrentTypeVersion, sagaProcessKey)
        {
            ProductAccess = access;
        }

    }
}
