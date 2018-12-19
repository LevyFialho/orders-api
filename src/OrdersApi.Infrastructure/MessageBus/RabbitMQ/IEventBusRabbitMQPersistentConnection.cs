using RabbitMQ.Client;
using System;

namespace OrdersApi.Infrastructure.MessageBus.RabbitMQ
{
    public interface IEventBusRabbitMQPersistentConnection : IDisposable
    {
        bool IsConnectedToEventBus { get; }

        bool TryConnectToEventBus();

        IModel CreateEventBusModel();
    }
}
