using System; 
using System.Threading.Tasks;
using OrdersApi.Cqrs.Models;

namespace OrdersApi.Cqrs.Repository
{
    
    public interface IRepository : IDisposable  
    {
        Task<T> GetByIdAsync<T>(string aggregateKey) where T : AggregateRoot;
        Task<T> GetAsync<T>(string correlationKey, string applicationKey) where T : AggregateRoot;
        Task SaveAsync<T>(T aggregate) where T : AggregateRoot;
    }
    
}
