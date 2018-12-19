using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Queries;
using MediatR;

namespace OrdersApi.Infrastructure.MessageBus
{
    [ExcludeFromCodeCoverage]
    public class QueryBus : IQueryBus
    {
        private readonly IMediator _mediator;

        public QueryBus(IMediator mediator)
        {
            _mediator = mediator;
        }

        public Task<TResponse> Send<TQuery, TResponse>(TQuery query) where TQuery : IQuery<TResponse>
        {
            return _mediator.Send(query);
        }

        public Task<PagedResult<TResponse>> SendPagedQuery<TQuery, TResponse>(TQuery query) where TQuery : PagedQuery<TResponse> where TResponse : class
        {
            return _mediator.Send(query);
        }

        public Task<SeekResult<TResponse>> SendSeekQuery<TQuery, TResponse>(TQuery query) where TQuery : SeekQuery<TResponse> where TResponse : class
        {
            return _mediator.Send(query);
        }
    }
}
