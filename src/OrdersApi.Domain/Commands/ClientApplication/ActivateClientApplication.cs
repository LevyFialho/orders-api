using System;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Models;
using FluentValidation.Results;

namespace OrdersApi.Domain.Commands.ClientApplication
{
    public class ActivateClientApplication : Command
    {  

        public ActivateClientApplication(string aggregateKey, string correlationKey, string applicationKey, string sagaProcessKey) 
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
