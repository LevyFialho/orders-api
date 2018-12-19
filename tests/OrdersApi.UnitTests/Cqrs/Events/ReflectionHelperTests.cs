using System;
using OrdersApi.Cqrs.Extensions;
using FluentAssertions;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Events
{
    public class ReflectionHelperTests
    {
        public class FindEventHandlerMethodsInAggregateTests
        {
            public class DuplicatedHandlerTestAggregate : TestAggregate
            {
                [InternalEventHandler]
                public void DuplicatedOnTestEvent(TestEvent @event)
                {
                    throw new NotImplementedException();
                }
            }

            [Fact]
            public void FindEventHandlerMethodsInAggregateTest()
            {  
                var eventHandlerCache = ReflectionHelper.FindEventHandlerMethodsInAggregate(typeof(TestAggregate));
                Assert.Single(eventHandlerCache);
            }
            [Fact]
            public void FindEventHandlerMethodsInAggregateFailForMultipleHandlersTest()
            {
                Action action = () => ReflectionHelper.FindEventHandlerMethodsInAggregate(typeof(DuplicatedHandlerTestAggregate));
                action.Should().Throw<System.AggregateException>();
            }
        }

    }
}
