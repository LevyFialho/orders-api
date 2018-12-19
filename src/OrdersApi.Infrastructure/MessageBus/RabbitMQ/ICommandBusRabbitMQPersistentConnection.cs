using RabbitMQ.Client;
using System;

namespace OrdersApi.Infrastructure.MessageBus.RabbitMQ
{
    public interface ICommandBusRabbitMQPersistentConnection : IDisposable
    {
        bool IsConnectedToCommandBus { get; }

        bool TryConnectToCommandBus();

        IModel CreateCommandBusModel();
    }
}
