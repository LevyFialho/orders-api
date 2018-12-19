using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq; 
using OrdersApi.Cqrs.Commands;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using MediatR;

namespace OrdersApi.Infrastructure.MessageBus.CommandBus
{
    [ExcludeFromCodeCoverage]
    public partial class InMemoryCommandBusSubscriptionsManager : ICommandBusSubscriptionsManager
    { 
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
        private readonly List<Type> _commandTypes;

        public event EventHandler<string> OnCommandRemoved;

        public InMemoryCommandBusSubscriptionsManager()
        {
            _handlers = new Dictionary<string, List<SubscriptionInfo>>();
            _commandTypes = new List<Type>();
        }

        public bool IsEmpty => !_handlers.Keys.Any();
        public void Clear() => _handlers.Clear();

        public void AddDynamicSubscription<TH>(string commandName)
            where TH : IDynamicIntegrationHandler
        {
            DoAddSubscription(typeof(TH), commandName, isDynamic: true);
        }

        public void AddSubscription<T, TH>()
            where T : ICommand
            where TH : IRequestHandler<T>
        {
            var commandName = GetCommandKey<T>();
            DoAddSubscription(typeof(TH), commandName, isDynamic: false);
            _commandTypes.Add(typeof(T));
        }

        private void DoAddSubscription(Type handlerType, string commandName, bool isDynamic)
        {
            if (!HasSubscriptionsForCommand(commandName))
            {
                _handlers.Add(commandName, new List<SubscriptionInfo>());
            }

            if (_handlers[commandName].Any(s => s.HandlerType == handlerType))
            {
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already registered for '{commandName}'", nameof(handlerType));
            }

            if (isDynamic)
            {
                _handlers[commandName].Add(SubscriptionInfo.Dynamic(handlerType));
            }
            else
            {
                _handlers[commandName].Add(SubscriptionInfo.Typed(handlerType));
            }
        }


        public void RemoveDynamicSubscription<TH>(string commandName)
            where TH : IDynamicIntegrationHandler
        {
            var handlerToRemove = FindDynamicSubscriptionToRemove<TH>(commandName);
            DoRemoveHandler(commandName, handlerToRemove);
        }


        public void RemoveSubscription<T, TH>()
            where TH : IRequestHandler<T>
            where T : ICommand
        {
            var handlerToRemove = FindSubscriptionToRemove<T, TH>();
            var commandName = GetCommandKey<T>();
            DoRemoveHandler(commandName, handlerToRemove);
        }


        private void DoRemoveHandler(string commandName, SubscriptionInfo subsToRemove)
        {
            if (subsToRemove != null)
            {
                _handlers[commandName].Remove(subsToRemove);
                if (!_handlers[commandName].Any())
                {
                    _handlers.Remove(commandName);
                    var commandType = _commandTypes.SingleOrDefault(e => e.Name == commandName);
                    if (commandType != null)
                    {
                        _commandTypes.Remove(commandType);
                    }
                    RaiseOnCommandRemoved(commandName);
                }

            }
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForCommand<T>() where T : ICommand
        {
            var key = GetCommandKey<T>();
            return GetHandlersForCommand(key);
        }
        public IEnumerable<SubscriptionInfo> GetHandlersForCommand(string commandName) => _handlers[commandName];

        private void RaiseOnCommandRemoved(string commandName)
        {
            var handler = OnCommandRemoved;
            if (handler != null)
            {
                OnCommandRemoved(this, commandName);
            }
        }


        private SubscriptionInfo FindDynamicSubscriptionToRemove<TH>(string commandName)
            where TH : IDynamicIntegrationHandler
        {
            return DoFindSubscriptionToRemove(commandName, typeof(TH));
        }


        private SubscriptionInfo FindSubscriptionToRemove<T, TH>()
             where T : ICommand
             where TH : IRequestHandler<T>
        {
            var commandName = GetCommandKey<T>();
            return DoFindSubscriptionToRemove(commandName, typeof(TH));
        }

        private SubscriptionInfo DoFindSubscriptionToRemove(string commandName, Type handlerType)
        {
            if (!HasSubscriptionsForCommand(commandName))
            {
                return null;
            }

            return _handlers[commandName].SingleOrDefault(s => s.HandlerType == handlerType);

        }

        public bool HasSubscriptionsForCommand<T>() where T : ICommand
        {
            var key = GetCommandKey<T>();
            return HasSubscriptionsForCommand(key);
        }
        public bool HasSubscriptionsForCommand(string commandName) => _handlers.ContainsKey(commandName);

        public Type GetCommandTypeByName(string commandName) => _commandTypes.SingleOrDefault(t => t.Name == commandName);

        public string GetCommandKey<T>()
        {
            return typeof(T).Name;
        }
    }
}
