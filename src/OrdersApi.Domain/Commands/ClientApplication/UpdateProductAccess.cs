using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Commands;

namespace OrdersApi.Domain.Commands.ClientApplication
{
    public class UpdateProductAccess : Command
    {
        public string ProductAggregateKey { get; set; }

        public bool CanCharge { get; set; }

        public bool CanQuery { get; set; } 

        public UpdateProductAccess(string aggregateKey, string correlationKey, string applicationKey, string productAggregateKey, bool canCharge, bool canQuery, string sagaProcessKey)
            : base(aggregateKey, correlationKey, applicationKey, sagaProcessKey)
        {
            ProductAggregateKey = productAggregateKey;
            CanCharge = canCharge;
            CanQuery = canQuery;
        }

        [ExcludeFromCodeCoverage]
        public override bool IsValid()
        {
            return true;
        }
    }
}
