using OrdersApi.Cqrs.Events;

namespace OrdersApi.Infrastructure.MessageBus.Abstractions
{
    public interface IEventBus
    {
        void Publish(IEvent @event);

        void Subscribe<T, TH>()
            where T : IEvent
            where TH : IEventHandler<T>;

        void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationHandler;

        void UnsubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationHandler;

        void Unsubscribe<T, TH>()
            where TH : IEventHandler<T>
            where T : IEvent;
    }
}
