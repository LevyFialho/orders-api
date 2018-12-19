using System;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Domain.Model.ClientApplicationAggregate; 

namespace OrdersApi.Contracts.V1.ClientApplication.Views
{
    [ExcludeFromCodeCoverage]
    public class ProductAccessView
    {
        public string ProductInternalKey { get; set; }

        public bool CanCharge { get; set; }

        public bool CanQuery { get; set; }

        public ProductAccessView()
        {
            
        }

        public ProductAccessView(ProductAccess access)
        {
            ProductInternalKey = access.ProductAggregateKey;
            CanQuery = access.CanQuery;
            CanCharge = access.CanCharge; 
        }
    }
}
