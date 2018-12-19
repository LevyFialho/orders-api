using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Commands; 

namespace OrdersApi.Domain.Commands.Charge.Reversal
{
    public class VerifyReversalSettlement : Command
    { 
        public string ReversalKey { get; set; }

        public VerifyReversalSettlement(string aggregateKey, string correlationKey, string applicationKey, 
            string sagaProcessKey, string reversalKey)
            : base(aggregateKey, correlationKey, applicationKey, sagaProcessKey)
        {
            ReversalKey = reversalKey;
        }
         
        public override bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ReversalKey) && !string.IsNullOrWhiteSpace(AggregateKey);
        }
    }
}
