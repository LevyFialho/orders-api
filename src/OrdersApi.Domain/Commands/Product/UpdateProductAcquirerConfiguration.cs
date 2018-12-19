using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Domain.Model.ProductAggregate;
#pragma warning disable S107

namespace OrdersApi.Domain.Commands.Product
{
    public class UpdateProductAcquirerConfiguration : Command
    {
        public AcquirerConfiguration Configuration { get; set; }

        public UpdateProductAcquirerConfiguration(string aggregateKey, string correlationKey, string applicationKey, 
            string sagaProcessKey, string acquirerKey, string accountKey, bool isEnabled)
            : base(aggregateKey, correlationKey, applicationKey, sagaProcessKey)
        {
           Configuration = new AcquirerConfiguration()
           {
               AcquirerKey = acquirerKey,
               CanCharge = isEnabled,
               AccountKey = accountKey
           };
        }

        [ExcludeFromCodeCoverage]
        public override bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Configuration?.AcquirerKey) && !string.IsNullOrWhiteSpace(Configuration?.AccountKey);
        }
    }
}
