using System; 
using Microsoft.Azure.ServiceBus;

namespace OrdersApi.Infrastructure.MessageBus.ServiceBus
{
    public interface IServiceBusPersisterConnection : IDisposable
    {
        ServiceBusConnectionStringBuilder EventBusConnectionStringBuilder { get; }

        ServiceBusConnectionStringBuilder CommandBusConnectionStringBuilder { get; }

        ITopicClient CreateModel();

        IQueueClient CreateQueueModel();
    }
}
