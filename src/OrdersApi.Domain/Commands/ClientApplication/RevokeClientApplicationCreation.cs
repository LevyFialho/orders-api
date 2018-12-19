using System;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Models;
using FluentValidation.Results;

namespace OrdersApi.Domain.Commands.ClientApplication
{
    public class RevokeClientApplicationCreation : Command
    {
        public string Reason { get; set; }

        public RevokeClientApplicationCreation(string aggregateKey, string correlationKey, string applicationKey, string reason, string sagaProcessKey)
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
