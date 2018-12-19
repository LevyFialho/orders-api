using System; 
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Model.ChargeAggregate; 
using Newtonsoft.Json; 

namespace OrdersApi.Contracts.V1.Charge.Commands
{ 
    public class RevertCharge : CommandRequest<Domain.Commands.Charge.Reversal.CreateChargeReversal>
    { 

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [JsonIgnore]
        protected string InternalApplicationKey { get; set; } 

        public void SetInternalApplicationKey(string key)
        {
            InternalApplicationKey = key;
        }

        [JsonIgnore]
        protected string ChargeKey { get; set; }

        public void SetChargeKey(string key)
        {
            ChargeKey = key;
        } 

        public override Domain.Commands.Charge.Reversal.CreateChargeReversal GetCommand()
        { 
            return new Domain.Commands.Charge.Reversal.CreateChargeReversal(ChargeKey,
                CorrelationKey.ToString("N"), InternalApplicationKey, IdentityGenerator.NewSequentialIdentity(), Amount);
        }
    }
}
