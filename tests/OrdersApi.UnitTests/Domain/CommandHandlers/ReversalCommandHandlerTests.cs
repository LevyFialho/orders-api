using System;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.CommandHandlers;
using OrdersApi.Domain.Commands.Charge;
using OrdersApi.Domain.Commands.Charge.Reversal;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Domain.Model.ChargeAggregate;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
#pragma warning disable CS1998

namespace OrdersApi.UnitTests.Domain.CommandHandlers
{
    public class ReversalCommandHandlerTests
    {
        public class HandleCreateChargeCommandTests
        {
            [Fact]
            public async void HandeInvalidCommand()
            { 
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var logger = new Mock<ILogger<ReversalCommandsHandler>>();
                var integrationService = new Mock<IAcquirerApiService>();
                var command = new Mock<CreateChargeReversal>(null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(false);
                var handler = new Mock<ReversalCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object){ CallBase = true};

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<AggregateRoot>()), Times.Never);
            }

            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var app = new Mock<Charge>();
                app.Setup(x => x.CanRevert(It.IsAny<decimal>())).Returns(true); 
                var integrationService = new Mock<IAcquirerApiService>();
                var logger = new Mock<ILogger<ReversalCommandsHandler>>();
                var command = new Mock<CreateChargeReversal>(null, null, null, null, null);
                eventStore.Setup(x => x.GetByIdAsync<Charge>(command.Object.AggregateKey)).Returns(Task.FromResult(app.Object));
                command.Setup(x => x.IsValid()).Returns(true);
                var handler = new Mock<ReversalCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };
                

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                app.Verify(x => x.CanRevert(It.IsAny<decimal>()), Times.Once);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<Charge>()), Times.Once);
            }

            [Fact]
            public async void HandeValidCommandWichCanNotRevert()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var app = new Mock<Charge>();
                app.Setup(x => x.CanRevert(It.IsAny<decimal>())).Returns(false);
                var integrationService = new Mock<IAcquirerApiService>();
                var logger = new Mock<ILogger<ReversalCommandsHandler>>();
                var command = new Mock<CreateChargeReversal>(null, null, null, null, null);
                eventStore.Setup(x => x.GetByIdAsync<Charge>(command.Object.AggregateKey)).Returns(Task.FromResult(app.Object));
                command.Setup(x => x.IsValid()).Returns(true);
                var handler = new Mock<ReversalCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };

          
                Func<Task> invocation = async () => await handler.Object.Handle(command.Object, CancellationToken.None);

                invocation.Should().Throw<CommandExecutionFailedException>();
                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                app.Verify(x => x.CanRevert(It.IsAny<decimal>()), Times.Once);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<Charge>()), Times.Never);
            }
        }

        public class ProcessAcquirerAccountReversalTests
        {
            [Fact]
            public async void HandeInvalidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var integrationService = new Mock<IAcquirerApiService>();
                var logger = new Mock<ILogger<ReversalCommandsHandler>>();
                var command = new Mock<ProcessAcquirerAccountReversal>(null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(false);
                var handler = new Mock<ReversalCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<AggregateRoot>()), Times.Never);
            }

            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var app = new Mock<Charge>();
                var bus = new Mock<IMessageBus>();
                var logger = new Mock<ILogger<ReversalCommandsHandler>>();
                var integrationService = new Mock<IAcquirerApiService>();
                var command = new Mock<ProcessAcquirerAccountReversal>(null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                eventStore.Setup(x => x.GetByIdAsync<Charge>(command.Object.AggregateKey)).Returns(Task.FromResult(app.Object));
                var handler = new Mock<ReversalCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                app.Verify(x => x.SendReversalToAcquirer(command.Object.CorrelationKey, command.Object.ApplicationKey, command.Object.SagaProcessKey, command.Object.ReversalKey, integrationService.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(app.Object), Times.Once);
            }
        }
         

        public class HandleVerifyReversalSettlementTests
        {
            [Fact]
            public async void HandeInvalidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var logger = new Mock<ILogger<ReversalCommandsHandler>>();
                var integrationService = new Mock<IAcquirerApiService>();
                var command = new Mock<VerifyReversalSettlement>(null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(false);
                var handler = new Mock<ReversalCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };
                await handler.Object.Handle(command.Object, CancellationToken.None);
                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<AggregateRoot>()), Times.Never);
            }

            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var app = new Mock<Charge>();
                var logger = new Mock<ILogger<ReversalCommandsHandler>>();
                var bus = new Mock<IMessageBus>();
                var integrationService = new Mock<IAcquirerApiService>();
                var command = new Mock<VerifyReversalSettlement>(null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                eventStore.Setup(x => x.GetByIdAsync<Charge>(command.Object.AggregateKey)).Returns(Task.FromResult(app.Object));
                var handler = new Mock<ReversalCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                app.Verify(x => x.VerifyReversalSettlement(command.Object.CorrelationKey, command.Object.ApplicationKey, command.Object.SagaProcessKey, command.Object.ReversalKey, integrationService.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(app.Object), Times.Once);
            }
        }
    }
}
