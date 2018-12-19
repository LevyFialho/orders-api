using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using AutoFixture;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Events;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage;
using MediatR;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Infrastructure.MessageBus
{
    public class MessageBusTests
    {
        public class TestCommand : ICommand
        {
            public string AggregateKey { get; set; }
            public string CommandKey { get; set; }
            public string ApplicationKey { get; set; }
            public string CorrelationKey { get; set; }
            public string SagaProcessKey { get; set; }
        }

        public class TestEvent : IEvent
        {
            public short TargetVersion { get; set; }
            public string SagaProcessKey { get; set; }
            public string EventKey { get; set; }
            public string AggregateKey { get; set; }
            public string ApplicationKey { get; set; }
            public string CorrelationKey { get; set; }
            public DateTime EventCommittedTimestamp { get; set; }
            public int ClassVersion { get; set; }
        }

        [Fact]
        public async void RaiseEventThroughEventBusTest()
        { 
            var mediator = new Mock<IMediator>();
            var eventBus = new Mock<IEventBus>(); 
            var eventLogservice = new Mock<IEventLogService>();
            var evt = new Fixture().Create<TestEvent>();

            var bus = new OrdersApi.Infrastructure.MessageBus.MessageBus(mediator.Object,  eventBus.Object,
                eventLogservice.Object);
            await bus.RaiseEvent(evt);

            eventBus.Verify(x => x.Publish(evt), Times.Once);
            mediator.Verify(x => x.Publish(evt, default(CancellationToken)), Times.Never);
            eventLogservice.Verify(x => x.MarkEventAsPublishedAsync(evt), Times.Once);
        }

        [Fact]
        public async void RaiseEventThroughMediatorTest()
        {
            var mediator = new Mock<IMediator>(); 
            var eventLogservice = new Mock<IEventLogService>();
            var evt = new Fixture().Create<TestEvent>();
            var commandBus = new Mock<ICommandBus>();

            var bus = new OrdersApi.Infrastructure.MessageBus.MessageBus(mediator.Object, null,
                eventLogservice.Object);
            await bus.RaiseEvent(evt);
             
            mediator.Verify(x => x.Publish(evt, default(CancellationToken)), Times.Once);
            eventLogservice.Verify(x => x.MarkEventAsPublishedAsync(evt), Times.Once);
        }

        [Fact]
        public async  void SendCommandCallsMediatorTest()
        {
            var mediator = new Mock<IMediator>();
            var eventBus = new Mock<IEventBus>();
            var commandBus = new Mock<ICommandBus>();
            var eventLogservice = new Mock<IEventLogService>();
            var command = new Fixture().Create<TestCommand>();

            var bus = new OrdersApi.Infrastructure.MessageBus.MessageBus(mediator.Object, eventBus.Object,
                eventLogservice.Object);
            await bus.SendCommand(command);

            mediator.Verify(x => x.Send(command, default(CancellationToken)), Times.Once);
        } 
    }
}
