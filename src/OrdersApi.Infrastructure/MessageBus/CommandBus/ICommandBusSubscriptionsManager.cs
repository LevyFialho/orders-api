using System;
using System.Collections.Generic;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using MediatR;

namespace OrdersApi.Infrastructure.MessageBus.CommandBus
{
    public interface ICommandBusSubscriptionsManager
    {
        bool IsEmpty { get; }
        event EventHandler<string> OnCommandRemoved;
        void AddDynamicSubscription<TH>(string commandName)
            where TH : IDynamicIntegrationHandler;

        void AddSubscription<T, TH>()
            where T : ICommand
            where TH : IRequestHandler<T>;

        void RemoveSubscription<T, TH>()
            where TH : IRequestHandler<T>
            where T : ICommand;
        void RemoveDynamicSubscription<TH>(string commandName)
            where TH : IDynamicIntegrationHandler;

        bool HasSubscriptionsForCommand<T>() where T : ICommand;
        bool HasSubscriptionsForCommand(string commandName);
        Type GetCommandTypeByName(string commandName);
        void Clear();
        IEnumerable<SubscriptionInfo> GetHandlersForCommand<T>() where T : ICommand;
        IEnumerable<SubscriptionInfo> GetHandlersForCommand(string commandName);
        string GetCommandKey<T>();
    }
}
