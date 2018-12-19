using System;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using OrdersApi.Domain.Model.Projections.ProductProjections;

namespace OrdersApi.Contracts.V1.Product.Views
{
    [ExcludeFromCodeCoverage]
    public class AcquirerConfigurationView
    {
        public string AcquirerKey { get; set; }

        public bool CanCharge { get; set; }

        public string AccountKey  { get; set; }

        public AcquirerConfigurationView()
        {
            
        }

        public AcquirerConfigurationView(AcquirerConfigurationProjection projection)
        {
            AcquirerKey = projection.AcquirerKey;
            AccountKey = projection.AccountKey;
            CanCharge = projection.CanCharge;
        }
    }
}
