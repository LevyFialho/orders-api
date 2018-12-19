using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Events;

namespace OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage
{
    public interface IEventLogService
    {
        IEnumerable<IEvent> GetUnpublishedEvents();
        Task MarkEventAsPublishedAsync(IEvent @event);
    }
}
