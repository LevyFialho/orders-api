using System;
using System.Reflection;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Exceptions;

namespace OrdersApi.Cqrs.Extensions
{ 
    public static class EventExtension
    {
        /// <summary>
        /// Chama através de reflection o método do aggregate responsável por aplicar o evento.
        /// </summary>
        /// <param name="event">Evento origem</param>
        /// <param name="aggregate">Aggregate no qual o evento será aplicado</param>
        /// <param name="methodName">Nome do método</param>
        public static void InvokeOnAggregate(this IEvent @event, AggregateRoot aggregate, string methodName)
        {
            var method = aggregate.GetType().GetRuntimeMethod(methodName, new Type[] { @event.GetType() }); //Find the right method

            if (method != null)
            {
                method.Invoke(aggregate, new object[] { @event }); //invoke with the event as argument
            }
            else
            {
                throw new AggregateEventOnApplyMethodMissingException($"No event Apply method found on {aggregate.GetType()} for {@event.GetType()}");
            }
        }
    }
}
