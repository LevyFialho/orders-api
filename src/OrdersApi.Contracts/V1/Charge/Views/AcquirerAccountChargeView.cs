using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Model.Projections.ChargeProjections;

namespace OrdersApi.Contracts.V1.Charge.Views
{
    [ExcludeFromCodeCoverage]
    public class AcquirerAccountChargeView
    {
        public string InternalKey { get; set; }

        public string CorrelationKey { get; set; }

        public decimal Amount { get; set; } 

        public DateTime CreatedDate { get; set; }

        public DateTime ChargeDate { get; set; }

        public string Status { get; set; }

        public string PaymentMethod { get; set; }

        public decimal AmountReverted { get; set; }

        public AcquirerAccountPaymentDetails PaymentDetails { get; set; }

        public ProductInfoView Product { get; set; }

        public AcquirerAccountChargeView()
        {
            
        }

        public AcquirerAccountChargeView(ChargeProjection projection)
        {
            InternalKey = projection.AggregateKey;
            CorrelationKey = projection.CorrelationKey;
            Amount = projection.OrderDetails?.Amount ?? 0m;
            AmountReverted = projection.AmountReverted;
            ChargeDate = projection.OrderDetails?.ChargeDate ?? default(DateTime);
            CreatedDate = projection.CreatedDate;
            Status = projection.Status.ToString();
            PaymentMethod = projection.Method.ToString();
            Product = new ProductInfoView()
            {
                Key = projection.Product?.ExternalKey,
                Name = projection.Product?.Name,
            };
            PaymentDetails = new AcquirerAccountPaymentDetails()
            {
               AccountType = projection.AcquirerAccount?.AccountType ?? 0,
               AcquirerKey = projection.AcquirerAccount?.AcquirerKey,
               MerchantKey = projection.AcquirerAccount?.MerchantKey
            }; 
        }
        
    }
}
