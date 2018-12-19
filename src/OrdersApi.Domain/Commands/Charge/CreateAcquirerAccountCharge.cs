using System;
using System.Collections.Generic;
using System.Linq;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Domain.Model.ChargeAggregate; 
using FluentValidation.Results;

namespace OrdersApi.Domain.Commands.Charge
{
    public class CreateAcquirerAccountCharge : Command
    {
        public OrderDetails OrderDetails { get; set; }

        public AcquirerAccount AcquirerAccount { get; set; }

        public CreateAcquirerAccountCharge(string aggregateKey, string correlationKey, string applicationKey, 
            string sagaProcessKey, OrderDetails orderDetails, AcquirerAccount acquirerAccount)
            : base(aggregateKey, correlationKey, applicationKey, sagaProcessKey)
        {
            OrderDetails = orderDetails ?? new OrderDetails();
            AcquirerAccount = acquirerAccount ?? new AcquirerAccount(); 
        }

        public override bool IsValid()
        {
            List<ValidationFailure> errorList = new List<ValidationFailure>();
            if (string.IsNullOrWhiteSpace(AcquirerAccount?.AcquirerKey))
                errorList.Add(new ValidationFailure("AcquirerKey", "Can not be empty"));
                
            if (string.IsNullOrWhiteSpace(AcquirerAccount?.MerchantKey))
                errorList.Add(new ValidationFailure("MerchantKey", "Can not be empty")); 
                
            if (string.IsNullOrWhiteSpace(OrderDetails?.ProductInternalKey))
                errorList.Add(new ValidationFailure("ProductInternalKey", "Can not be empty"));
              
            if (OrderDetails == null || OrderDetails.Amount < 0.01m)
                errorList.Add(new ValidationFailure("Amount", "Can not be smaller than 0.01"));
             
            if (OrderDetails == null || OrderDetails.ChargeDate < DateTime.UtcNow.Date.AddDays(1))
                errorList.Add(new ValidationFailure("ChargeDate", "Can not be smaller than " + DateTime.UtcNow.Date.AddDays(1).ToString("s")));
             
            if (errorList.Any())
            {
                ValidationResult = new ValidationResult(errorList.ToArray());
                return false;
            }
            return true;
        }
    }
}
