using System;
using OrdersApi.Cqrs.Extensions;
using OrdersApi.Cqrs.Models;

namespace OrdersApi.UnitTests.Cqrs.Events
{
    public class TestAggregate : AggregateRoot
    {
        [InternalEventHandler]
        public void OnTestEvent(TestEvent @event)
        {
            throw new NotImplementedException();
        }
    }

   
}
