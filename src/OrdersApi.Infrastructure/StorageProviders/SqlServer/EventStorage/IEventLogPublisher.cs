using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage
{
    public interface IEventLogPublisher
    {
        Task ProcessUnpublishedEvents();
    }
}
