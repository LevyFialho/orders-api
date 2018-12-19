using System;

namespace OrdersApi.Cqrs.Extensions
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class InternalEventHandlerAttribute : Attribute
    {
    }
}
