using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Domain.Model.ProductAggregate
{
    public class AcquirerConfiguration
    {
        public string AcquirerKey { get; set; }

        public bool CanCharge { get; set; }

        public string AccountKey { get; set; } 
    }
}
