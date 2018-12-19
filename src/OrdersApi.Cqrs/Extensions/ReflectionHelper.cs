using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OrdersApi.Cqrs.Models; 
using OrdersApi.Cqrs.Events;

namespace OrdersApi.Cqrs.Extensions
{
    /// <summary>
    /// Helpers de reflection para achar os métodos que aplicam os eventos ao aggregate
    /// </summary>
    public static class ReflectionHelper
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, string>> AggregateEventHandlerCache =
            new ConcurrentDictionary<Type, ConcurrentDictionary<Type, string>>();

        public static Dictionary<Type, string> FindEventHandlerMethodsInAggregate(Type aggregateType)
        {
            if (!AggregateEventHandlerCache.ContainsKey(aggregateType))
            {
                var eventHandlers = new ConcurrentDictionary<Type, string>();

                var methods = aggregateType.GetMethodsBySig(typeof(void), typeof(InternalEventHandlerAttribute), true, typeof(IEvent)).ToList();

                if (methods.Any())
                {
                    foreach (var m in methods)
                    {
                        var parameter = m.GetParameters().First();
                        if (!eventHandlers.TryAdd(parameter.ParameterType, m.Name))
                        {
                            throw new AggregateException($"Multiple methods found handling same event in {aggregateType.Name}");
                        }
                    }
                }
                AggregateEventHandlerCache.TryAdd(aggregateType, eventHandlers);
            }


            return AggregateEventHandlerCache[aggregateType].ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static IEnumerable<MethodInfo> GetMethodsBySig(this Type type,
                                                               Type returnType,
                                                               Type customAttributeType,
                                                               bool matchParameterInheritence,
                                                               params Type[] parameterTypes)
        {
            return type.GetRuntimeMethods().Where((m) =>
            {
                if (m.ReturnType != returnType) return false;

                if ((customAttributeType != null) && (!m.GetCustomAttributes(customAttributeType, true).Any()))
                    return false;

                var parameters = m.GetParameters();

                if ((parameterTypes == null || parameterTypes.Length == 0))
                    return parameters.Length == 0;

                if (parameters.Length != parameterTypes.Length)
                    return false;

                return !parameterTypes.Where((t, i) => !(parameters[i].ParameterType == t || matchParameterInheritence && t.GetTypeInfo().IsAssignableFrom(parameters[i].ParameterType.GetTypeInfo()))).Any();
            });
        }
    
 

        public static T CreateInstance<T>() where T : AggregateRoot
        {
            return (T)Activator.CreateInstance(typeof(T));
        }
    }
}
