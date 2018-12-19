using MediatR;

namespace OrdersApi.Cqrs.Commands
{
    public interface ICommandView<TCommand>: IRequest where TCommand : Command
    {
        TCommand GetCommand();
    }
}
