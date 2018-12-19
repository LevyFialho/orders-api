using System;
using System.Collections.Generic; 
using OrdersApi.Cqrs.Events;
using OrdersApi.Infrastructure.MessageBus.Abstractions;

namespace OrdersApi.Infrastructure.MessageBus.EventBus
{
    public interface IEventBusSubscriptionsManager
    {
        bool IsEmpty { get; }
        event EventHandler<string> OnEventRemoved;
        void AddDynamicSubscription<TH>(string eventName)
            where TH : IDynamicIntegrationHandler;

        void AddSubscription<T, TH>()
            where T : IEvent
            where TH : IEventHandler<T>;

        void RemoveSubscription<T, TH>()
            where TH : IEventHandler<T>
            where T : IEvent;
        void RemoveDynamicSubscription<TH>(string eventName)
            where TH : IDynamicIntegrationHandler;

        bool HasSubscriptionsForEvent<T>() where T : IEvent;
        bool HasSubscriptionsForEvent(string eventName);
        Type GetEventTypeByName(string eventName);
        void Clear();
        IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IEvent;
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);
        string GetEventKey<T>();
    }
}
