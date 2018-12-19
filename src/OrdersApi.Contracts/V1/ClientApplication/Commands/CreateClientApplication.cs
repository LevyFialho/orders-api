using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Commands.ClientApplication;

namespace OrdersApi.Contracts.V1.ClientApplication.Commands
{ 
    public class CreateClientApplication : CommandRequest<Domain.Commands.ClientApplication.CreateClientApplication>
    {
        [Required(AllowEmptyStrings = false)]
        public string ExternalId { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }
         

        public override Domain.Commands.ClientApplication.CreateClientApplication GetCommand()
        {
            return new Domain.Commands.ClientApplication.CreateClientApplication(IdentityGenerator.NewSequentialIdentity(),
                CorrelationKey.ToString("N"), IdentityGenerator.DefaultApplicationKey(), ExternalId, Name, IdentityGenerator.NewSequentialIdentity());
        }
    }
}
