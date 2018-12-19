using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Commands;

namespace OrdersApi.Domain.Commands.Product
{
    public class RevokeProductCreation : Command
    {
        public string Reason { get; set; }

        public RevokeProductCreation(string aggregateKey, string correlationKey, string applicationKey, string reason, string sagaProcessKey)
            : base(aggregateKey, correlationKey, applicationKey, sagaProcessKey)
        {
            Reason = reason;
        }

        [ExcludeFromCodeCoverage]
        public override bool IsValid()
        {
            return true;
        }
    }
}
