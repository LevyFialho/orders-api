using System;
using System.Collections.Generic;
using System.Text;
using AutoFixture;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage;
using OrdersApi.UnitTests.Cqrs.Events;
using Xunit;

namespace OrdersApi.UnitTests.Infrastructure.SqlEventStore
{
    public class EventDataExtensionsTests
    {
        [Fact]
        public void EventSerializationTest()
        {
            var evt = new Fixture().Create<TestEvent>();
            var value = evt.ToEventData(typeof(TestEvent)).DeserializeEvent();
            Assert.Equal(evt.CorrelationKey, value.CorrelationKey);
            Assert.Equal(evt.AggregateKey, value.AggregateKey);
            Assert.Equal(evt.ApplicationKey, value.ApplicationKey);
            Assert.Equal(evt.SagaProcessKey, value.SagaProcessKey);
            Assert.Equal(evt.ClassVersion, value.ClassVersion);
            Assert.Equal(evt.EventKey, value.EventKey);
            Assert.Equal(evt.TargetVersion, value.TargetVersion);
            Assert.Equal(evt.EventCommittedTimestamp, value.EventCommittedTimestamp);
        }

        [Fact]
        public void EventPayloadTest()
        {
            var evt = new Fixture().Create<TestEvent>();
            var payload = evt.ToEventData(typeof(TestEvent)).GetPayload();
            var value = payload.DeserializeEvent();
            Assert.Equal(evt.CorrelationKey, value.CorrelationKey);
            Assert.Equal(evt.AggregateKey, value.AggregateKey);
            Assert.Equal(evt.ApplicationKey, value.ApplicationKey);
            Assert.Equal(evt.SagaProcessKey, value.SagaProcessKey);
            Assert.Equal(evt.ClassVersion, value.ClassVersion);
            Assert.Equal(evt.EventKey, value.EventKey);
            Assert.Equal(evt.TargetVersion, value.TargetVersion);
            Assert.Equal(evt.EventCommittedTimestamp, value.EventCommittedTimestamp);
        }
    }
}
