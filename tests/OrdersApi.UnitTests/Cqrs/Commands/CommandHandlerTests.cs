using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Repository;
using FluentAssertions;
using FluentValidation.Results;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Commands
{
    public class CommandHandlerTests
    { 
        [Fact]
        public void NotifyValidationErrorsTests()
        {
            var bus = new Mock<IMessageBus>();
            var snapshotProvider = new Mock<ISnapshotStorageProvider>();
            var eventStorageProvider = new Mock<IEventStorageProvider>();
            var eventPublisher = new Mock<IMessageBus>();
            var eventStore = new Mock<AggregateDataSource>(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object);
            var handler = new Mock<CommandHandler>(eventStore.Object,bus.Object) {CallBase = true};
            var message = new Mock<Command>("A", "B", "X", "Z") {CallBase = true};
            bus.Setup(x => x.RaiseEvent(It.IsAny<IEvent>())).Verifiable();
            message.Setup(x => x.ValidationResult).Returns(new ValidationResult());
            handler.Object.NotifyValidationErrors(message.Object);
            bus.Verify(x => x.RaiseEvent(It.IsAny<IEvent>()), Times.Never);
        }
        [Fact]
        public void NotifyValidationErrorsRaisesEventsTests()
        {
            var bus = new Mock<IMessageBus>();
            var snapshotProvider = new Mock<ISnapshotStorageProvider>();
            var eventStorageProvider = new Mock<IEventStorageProvider>();
            var eventPublisher = new Mock<IMessageBus>();
            var eventStore = new Mock<AggregateDataSource>(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object);
            var handler = new Mock<CommandHandler>(eventStore.Object, bus.Object) { CallBase = true };
            var message = new Mock<Command>("a", "b", "X", "Z") { CallBase = true };
            bus.Setup(x => x.RaiseEvent(It.IsAny<IEvent>())).Verifiable();
            message.Setup(x => x.ValidationResult).Returns(new ValidationResult(){ Errors = { new ValidationFailure("","")}});
            handler.Object.NotifyValidationErrors(message.Object);
            bus.Verify(x => x.RaiseEvent(It.IsAny<IEvent>()), Times.Once);
        }

        [Fact]
        public async void ValidateDuplicateCommandTest()
        {
            var bus = new Mock<IMessageBus>();
            var snapshotProvider = new Mock<ISnapshotStorageProvider>();
            var eventStorageProvider = new Mock<IEventStorageProvider>();
            var message = new Mock<Command>("A", "B", "X", "Z") { CallBase = true };
            var eventStore = new Mock<AggregateDataSource>(eventStorageProvider.Object, snapshotProvider.Object, bus.Object);
            eventStore.Setup(x => x.GetAsync(message.Object.CorrelationKey, message.Object.ApplicationKey)).Returns(Task.FromResult(new List<IEvent>()));
            var handler = new Mock<CommandHandler>(eventStore.Object, bus.Object) { CallBase = true };
            await handler.Object.ValidateDuplicateCommand(message.Object);
            eventStore.Verify(x => x.GetAsync(message.Object.CorrelationKey, message.Object.ApplicationKey), Times.Once);
        }

        [Fact]
        public void ValidateDuplicateCommandShouldThrowDuplicateExceptionTest()
        {
            var bus = new Mock<IMessageBus>();
            var snapshotProvider = new Mock<ISnapshotStorageProvider>();
            var eventStorageProvider = new Mock<IEventStorageProvider>();
            var message = new Mock<Command>("A", "B", "X", "Z") { CallBase = true };
            var eventStore = new Mock<AggregateDataSource>(eventStorageProvider.Object, snapshotProvider.Object, bus.Object);
            eventStore.Setup(x => x.GetAsync(message.Object.CorrelationKey, message.Object.ApplicationKey)).Returns(Task.FromResult(new List<IEvent>() {new Event()}));
            var handler = new Mock<CommandHandler>(eventStore.Object, bus.Object) { CallBase = true };
            Func<Task> invocation = async () =>  await handler.Object.ValidateDuplicateCommand(message.Object);
            invocation.Should().Throw<DuplicateException>();
            eventStore.Verify(x => x.GetAsync(message.Object.CorrelationKey, message.Object.ApplicationKey), Times.Once);
        }

         

    }
}
