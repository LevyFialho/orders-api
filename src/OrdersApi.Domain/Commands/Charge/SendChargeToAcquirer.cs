using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Commands; 

namespace OrdersApi.Domain.Commands.Charge
{
    public class SendChargeToAcquirer : Command
    { 

        public SendChargeToAcquirer(string aggregateKey, string correlationKey, string applicationKey, 
            string sagaProcessKey)
            : base(aggregateKey, correlationKey, applicationKey, sagaProcessKey)
        { 
        }

        [ExcludeFromCodeCoverage]
        public override bool IsValid()
        {
            return true;
        }
    }
}
