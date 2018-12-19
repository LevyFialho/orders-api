using System;
using System.ComponentModel.DataAnnotations;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Commands.ClientApplication;
using Newtonsoft.Json;

namespace OrdersApi.Contracts.V1.Product.Commands
{
    public class CreateProductAcquirerConfiguration : CommandRequest<Domain.Commands.Product.UpdateProductAcquirerConfiguration>
    {
        [Required(AllowEmptyStrings = false)]
        public string AcquirerKey { get; set; }

        [Required]
        public bool CanCharge { get; set; }

        [Required]
        public string AccountKey { get; set; }

        [JsonIgnore]
        protected string AggregateKey { get; set; }

        public void SetAggregateKey(string key)
        {
            AggregateKey = key;
        }

        public override Domain.Commands.Product.UpdateProductAcquirerConfiguration GetCommand()
        {
            return new Domain.Commands.Product.UpdateProductAcquirerConfiguration(AggregateKey,
                CorrelationKey.ToString("N"),
                IdentityGenerator.DefaultApplicationKey(), IdentityGenerator.NewSequentialIdentity(), AcquirerKey,
                AccountKey, CanCharge);
        }
    }
}
