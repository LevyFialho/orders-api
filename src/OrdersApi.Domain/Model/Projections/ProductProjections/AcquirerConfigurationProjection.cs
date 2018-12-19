using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Domain.Model.ProductAggregate;

namespace OrdersApi.Domain.Model.Projections.ProductProjections
{
    public class AcquirerConfigurationProjection
    {
        public string AcquirerKey { get; set; }

        public bool CanCharge { get; set; }

        public string AccountKey { get; set; }

        public AcquirerConfigurationProjection()
        {
            
        }

        public AcquirerConfigurationProjection(AcquirerConfiguration config)
        {
            AcquirerKey = config.AcquirerKey;
            CanCharge = config.CanCharge;
            AccountKey = config.AccountKey;
        }
    }
}
