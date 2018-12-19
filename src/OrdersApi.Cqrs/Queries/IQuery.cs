using MediatR;

namespace OrdersApi.Cqrs.Queries
{
    public interface IQuery<out TResponse> : IRequest<TResponse>
    {
    }
}
