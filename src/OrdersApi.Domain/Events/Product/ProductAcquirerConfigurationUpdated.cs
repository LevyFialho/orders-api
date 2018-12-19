using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Cqrs.Events;
using OrdersApi.Domain.Model.ProductAggregate;

namespace OrdersApi.Domain.Events.Product
{
    public class ProductAcquirerConfigurationUpdated : Event
    {
        private static int _currrentTypeVersion = 1; 
         
        public AcquirerConfiguration Configuration { get; set; }

        public ProductAcquirerConfigurationUpdated(string aggregateKey, string correlationKey, 
            string applicationKey, short version,   string sagaProcessKey, AcquirerConfiguration configuration)
            : base(aggregateKey, correlationKey, applicationKey, version, _currrentTypeVersion, sagaProcessKey)
        {
            Configuration = configuration;
        }

    }
}
