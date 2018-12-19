using System;
using System.Reflection;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Extensions;
using FluentAssertions;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Events
{
    public class EventExtensionTests
    {
        
        [Fact] 
        public void InvokeOnAggregateTest()
        {
            var aggregate = new TestAggregate();
            var myEvent = new TestEvent();
            Action invocation = () =>  myEvent.InvokeOnAggregate(aggregate, "OnTestEvent");
            invocation.Should().Throw<TargetInvocationException>(); 
        }

        [Fact]
        public void InvokeOnAggregateFailedTest()
        {
            var aggregate = new TestAggregate();
            var myEvent = new FailedTestEvent();
            Action invocation = () => myEvent.InvokeOnAggregate(aggregate, "OnTestEvent");
            invocation.Should().Throw<AggregateEventOnApplyMethodMissingException>();
        }
    }
}
