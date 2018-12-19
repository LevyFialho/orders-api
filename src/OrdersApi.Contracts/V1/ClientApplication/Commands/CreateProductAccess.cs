using System;
using System.ComponentModel.DataAnnotations;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Commands.ClientApplication;
using Newtonsoft.Json;

namespace OrdersApi.Contracts.V1.ClientApplication.Commands
{
    public class CreateProductAccess : CommandRequest<UpdateProductAccess>
    {
        [Required(AllowEmptyStrings = false)]
        public string ProductInternalKey { get; set; }

        [Required]
        public bool CanCharge { get; set; }

        [Required]
        public bool CanQuery { get; set; }

        [JsonIgnore]
        public string AggregateKey { get; set; } 

        public override UpdateProductAccess GetCommand()
        {
            return new UpdateProductAccess(AggregateKey, CorrelationKey.ToString("N"),
                IdentityGenerator.DefaultApplicationKey(), ProductInternalKey, CanCharge, CanQuery, IdentityGenerator.NewSequentialIdentity());
        }
    }
}
