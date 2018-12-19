using System; 
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Model.ChargeAggregate; 
using Newtonsoft.Json;

namespace OrdersApi.Contracts.V1.Charge.Commands
{ 
    public class CreateAcquirerAccountCharge : CommandRequest<Domain.Commands.Charge.CreateAcquirerAccountCharge>
    { 

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime ChargeDate { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string ProductExternalKey { get; set; }


        [Required]
        public AcquirerAccountDetails Payment { get; set; }

        [JsonIgnore]
        protected string InternalApplicationKey { get; set; }

        [JsonIgnore]
        protected string InternalProductKey { get; set; }

        public void SetInternalApplicationKey(string key)
        {
            InternalApplicationKey = key;
        }

        public void SetInternalProductKey(string key)
        {
            InternalProductKey = key;
        }

        public override Domain.Commands.Charge.CreateAcquirerAccountCharge GetCommand()
        {
            var orderDetails = new OrderDetails()
            {
                ChargeDate = ChargeDate,
                Amount = Amount,
                ProductInternalKey = InternalProductKey
            };
            var paymentInfo = new AcquirerAccount()
            { 
                AcquirerKey = Payment.AcquirerKey,
                MerchantKey = Payment.MerchantKey
            };
            return new Domain.Commands.Charge.CreateAcquirerAccountCharge(IdentityGenerator.NewSequentialIdentity(),
                CorrelationKey.ToString("N"), InternalApplicationKey, IdentityGenerator.NewSequentialIdentity(),
                orderDetails, paymentInfo);
        }
    }
}
