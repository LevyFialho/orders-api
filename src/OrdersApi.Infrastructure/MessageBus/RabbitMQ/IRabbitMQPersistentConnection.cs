using RabbitMQ.Client;
using System;
namespace OrdersApi.Infrastructure.MessageBus.RabbitMQ
{
    public interface IRabbitMQPersistentConnection : IDisposable
    {
        bool IsConnectedToEventBus { get; }

        bool TryConnectToEventBus();

        IModel CreateEventBusModel();

        bool IsConnectedToCommandBus { get; }

        bool TryConnectToCommandBus();

        IModel CreateCommandBusModel();
    }
}
