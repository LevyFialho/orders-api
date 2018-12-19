using System;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Models;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Events
{
    public class EventTests
    {
        [Fact]
        public void ConstructorTest()
        {
            var aggregateId = Guid.NewGuid().ToString();
            var correlationId = Guid.NewGuid().ToString();
            string applicationKey = "x";
            short targetVersion = 1;
            int eventClassVersion = 1;
            string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
            var myEvent = new Event(aggregateId, correlationId, applicationKey, targetVersion, eventClassVersion, sagaProcessKey);
            Assert.Equal(aggregateId, myEvent.AggregateKey);
            Assert.Equal(targetVersion, myEvent.TargetVersion);
            Assert.Equal(eventClassVersion, myEvent.ClassVersion);
            Assert.Equal(correlationId, myEvent.CorrelationKey);
            Assert.Equal(applicationKey, myEvent.ApplicationKey);
            Assert.Equal(sagaProcessKey, myEvent.SagaProcessKey);
        }
    }
}
