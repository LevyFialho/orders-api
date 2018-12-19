using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage;
using OrdersApi.UnitTests.Cqrs.Events;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Infrastructure.SqlEventStore
{
    public class EventLogPublisherTests
    {  
        [Fact]
        public async void ProcessUnpublishedEventsTest()
        {
            var eventLogService = new Mock<IEventLogService>();
            var messageBus = new Mock<IMessageBus>();
            var evt = new TestEvent();
            eventLogService.SetupSequence(x => x.GetUnpublishedEvents()).Returns(new List<TestEvent>() { evt }).Returns(new List<TestEvent>());

            var publisher = new EventLogPublisher(eventLogService.Object, messageBus.Object);
            await publisher.ProcessUnpublishedEvents();

            messageBus.Verify(x => x.RaiseEvent(It.IsAny<IEvent>()), Times.Once);
            eventLogService.Verify(x => x.MarkEventAsPublishedAsync(It.IsAny<IEvent>()), Times.Once);

        }
         
    }
}
