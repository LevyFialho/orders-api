using OrdersApi.Cqrs.Commands; 
using FluentValidation.Results;

namespace OrdersApi.Domain.Commands.Product
{
    public class CreateProduct : Command
    { 
        public string ExternalId { get; set; }

        public string Name { get; set; }
         

        public CreateProduct(string aggregateKey, string correlationKey, string applicationKey, string externalId, string name, string sagaProcessKey) 
            : base(aggregateKey, correlationKey, applicationKey, sagaProcessKey)
        {
            ExternalId = externalId;
            Name = name; 
        }

        public override bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(ExternalId))
            {
                ValidationResult = new ValidationResult(new[] { new ValidationFailure("ExternalId", "Can not be empty") });
                return false;
            }
            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationResult = new ValidationResult(new[] { new ValidationFailure("Name", "Can not be empty") });
                return false;
            } 
            return true;
        }
    }
}
