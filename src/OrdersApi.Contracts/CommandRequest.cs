using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Commands;

namespace OrdersApi.Contracts
{
    [ExcludeFromCodeCoverage]
    public abstract class CommandRequest<TCommand> : ClientBoundRequest, ICommandView<TCommand> where TCommand : Command
    { 
        [Required(ErrorMessage = "Invalid or empty value for CorrelationKey", AllowEmptyStrings = false)] 
        public Guid CorrelationKey { get; set; }

        public abstract TCommand GetCommand();
    }
}
