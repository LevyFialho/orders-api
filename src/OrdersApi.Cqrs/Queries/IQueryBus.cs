using System.Threading.Tasks;

namespace OrdersApi.Cqrs.Queries
{
    public interface IQueryBus
    {
        Task<TResponse> Send<TQuery, TResponse>(TQuery query) where TQuery : IQuery<TResponse>;

        Task<PagedResult<TResponse>> SendPagedQuery<TQuery, TResponse>(TQuery query) where TQuery : PagedQuery<TResponse> where TResponse : class;


        Task<SeekResult<TResponse>> SendSeekQuery<TQuery, TResponse>(TQuery query) where TQuery : SeekQuery<TResponse> where TResponse : class;
    }
}
