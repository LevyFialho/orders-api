using System;
using System.Collections.Generic;
using System.Linq;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Model.ChargeAggregate; 
using FluentValidation.Results;

namespace OrdersApi.Domain.Commands.Charge.Reversal
{
    public class CreateChargeReversal : Command
    {
        public decimal Amount { get; set; }
        
        public string ReversalKey { get; set; }

        public CreateChargeReversal(string aggregateKey, string correlationKey, string applicationKey, 
            string sagaProcessKey, decimal amount)
            : base(aggregateKey, correlationKey, applicationKey, sagaProcessKey)
        {
            Amount = amount;
            ReversalKey = IdentityGenerator.NewSequentialIdentity();
        }

        public override bool IsValid()
        {
            List<ValidationFailure> errorList = new List<ValidationFailure>();  
              
            if (Amount <= 0)
                errorList.Add(new ValidationFailure("Amount", "Can not be smaller than 0.01"));

            if (string.IsNullOrWhiteSpace(AggregateKey))
                errorList.Add(new ValidationFailure("AggregateKey", "Can not be empty"));

            if (string.IsNullOrWhiteSpace(CorrelationKey))
                errorList.Add(new ValidationFailure("CorrelationKey", "Can not be empty"));

            if (errorList.Any())
            {
                ValidationResult = new ValidationResult(errorList.ToArray());
                return false;
            }
            return true;
        }
    }
}
