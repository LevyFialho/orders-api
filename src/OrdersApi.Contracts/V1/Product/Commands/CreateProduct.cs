using System.ComponentModel.DataAnnotations;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Commands.Product;

namespace OrdersApi.Contracts.V1.Product.Commands
{
    public class CreateProduct : CommandRequest<Domain.Commands.Product.CreateProduct>
    {
        [Required(AllowEmptyStrings = false)]
        public string ExternalId { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }
         

        public override Domain.Commands.Product.CreateProduct GetCommand()
        {
            return new Domain.Commands.Product.CreateProduct(IdentityGenerator.NewSequentialIdentity(),
                                            CorrelationKey.ToString("N"),
                                            IdentityGenerator.DefaultApplicationKey(),
                                            ExternalId,
                                            Name, IdentityGenerator.NewSequentialIdentity());
        }
    }
}
