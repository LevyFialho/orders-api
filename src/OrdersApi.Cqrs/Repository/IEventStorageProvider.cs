using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Events;

namespace OrdersApi.Cqrs.Repository
{
    public interface IEventStorageProvider : IDisposable
    {
        Task<IEnumerable<IEvent>> GetEventsAsync(string aggregateKey, int start, int count);
        Task<IEnumerable<IEvent>> GetEventsAsync(string aggregateKey, int start, int count, Type aggregateType);
        Task<IEnumerable<IEvent>> GetEventsAsync(string correlationKey, string applicationKey, int start, int count);
        Task<IEnumerable<IEvent>> GetEventsAsync(string correlationKey, string applicationKey, int start, int count, Type aggregateType);
        Task<IEvent> GetLastEventAsync(string aggregateKey);
        Task CommitChangesAsync(AggregateRoot aggregate); 
    }
}


