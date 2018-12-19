using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Domain.Model.Projections.ChargeProjections
{
    public class ProductInfo
    { 
        public static ProductInfo GetProductInfo(ProductProjection existingProduct)
        {
            if (existingProduct == null)
                return null;

            return new ProductInfo
            {
                ExternalKey = existingProduct.ExternalKey,
                AggregateKey = existingProduct.AggregateKey,
                Name = existingProduct.Name
            }; 
        } 

        public string ExternalKey { get; set; }

        public string AggregateKey { get; set; }

        public string Name { get; set; }
    }
}
