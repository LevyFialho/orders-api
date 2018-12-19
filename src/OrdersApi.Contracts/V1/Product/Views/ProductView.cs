using System;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Domain.Model.Projections;

namespace OrdersApi.Contracts.V1.Product.Views
{
    [ExcludeFromCodeCoverage]
    public class ProductView
    {
        public string ExternalKey { get; set; }

        public string InternalKey { get; set; }

        public string Name { get; set; }

        public string Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public ProductView()
        {
            
        }

        public ProductView(ProductProjection app)
        {
            ExternalKey = app.ExternalKey;
            InternalKey = app.AggregateKey;
            Name = app.Name;
            CreatedDate = app.CreatedDate;
            Status = app.Status.ToString();

            if(!string.IsNullOrWhiteSpace(app.RejectionReason)) Status += " - " + app.RejectionReason;
        }
    }
}
