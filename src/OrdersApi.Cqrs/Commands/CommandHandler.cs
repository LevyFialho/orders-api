using System.Linq;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using MediatR;

namespace OrdersApi.Cqrs.Commands
{
    public abstract class CommandHandler
    {
        protected readonly IMessageBus Bus;
        protected readonly AggregateDataSource Repository;

        protected CommandHandler(AggregateDataSource repository, IMessageBus bus)
        {
            Bus = bus;
            Repository = repository;
        }

        public virtual void NotifyValidationErrors(Command message)
        {
            if (message?.ValidationResult?.Errors != null)
                foreach (var error in message?.ValidationResult?.Errors)
                {
                    Bus.RaiseEvent(new DomainNotification(message?.GetType().Name, error.ErrorMessage));
                }
        }

        public virtual async Task ValidateDuplicateCommand(ICommand command)  
        { 
            var duplicateRequest = await Repository.GetAsync(command.CorrelationKey, command.ApplicationKey);
            if (duplicateRequest != null && duplicateRequest.Any())
            {
                throw new DuplicateException() { OrignalAggregateKey = duplicateRequest.FirstOrDefault()?.AggregateKey };
            }
        }

    }
}
