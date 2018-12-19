using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Cqrs.Events;

namespace OrdersApi.Domain.Events.Product
{
    public class ProductCreated : Event
    {
        private static int _currrentTypeVersion = 1;

        public string ExternalKey { get; set; }

        public string Name { get; set; }

        public ProductCreated(string aggregateKey, string correlationKey, string applicationKey,
            string externalKey,  string name, short version, string sagaProcessKey)
            : base(aggregateKey, correlationKey, applicationKey, version, _currrentTypeVersion, sagaProcessKey)
        {
            ExternalKey = externalKey;
            Name = name; 
        }

    }
}
