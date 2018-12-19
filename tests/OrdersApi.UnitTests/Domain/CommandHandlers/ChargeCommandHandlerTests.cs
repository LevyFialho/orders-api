using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.CommandHandlers;
using OrdersApi.Domain.Commands.Charge;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Domain.Model.ChargeAggregate;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Domain.CommandHandlers
{
    public class ChargeCommandHandlerTests
    {
        public class HandleCreateChargeCommandTests
        {
            [Fact]
            public async void HandeInvalidCommand()
            { 
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var logger = new Mock<ILogger<ChargeCommandsHandler>>();
                var integrationService = new Mock<IAcquirerApiService>();
                var command = new Mock<CreateAcquirerAccountCharge>(null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(false);
                var handler = new Mock<ChargeCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object){ CallBase = true};

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<AggregateRoot>()), Times.Never);
            }

            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var integrationService = new Mock<IAcquirerApiService>();
                var logger = new Mock<ILogger<ChargeCommandsHandler>>();
                var command = new Mock<CreateAcquirerAccountCharge>(null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                var handler = new Mock<ChargeCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<Charge>()), Times.Once);
            } 
        }

        public class HandleSendChargeToAcquirerTests
        {
            [Fact]
            public async void HandeInvalidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var integrationService = new Mock<IAcquirerApiService>();
                var logger = new Mock<ILogger<ChargeCommandsHandler>>();
                var command = new Mock<SendChargeToAcquirer>(null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(false);
                var handler = new Mock<ChargeCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };

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
                var logger = new Mock<ILogger<ChargeCommandsHandler>>();
                var integrationService = new Mock<IAcquirerApiService>();
                var command = new Mock<SendChargeToAcquirer>(null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                eventStore.Setup(x => x.GetByIdAsync<Charge>(command.Object.AggregateKey)).Returns(Task.FromResult(app.Object));
                var handler = new Mock<ChargeCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                app.Verify(x => x.SendToAcquirer(command.Object.CorrelationKey, command.Object.ApplicationKey, command.Object.SagaProcessKey, integrationService.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(app.Object), Times.Once);
            }
        }

        public class HandleExpireChargeTests
        {
            [Fact]
            public async void HandeInvalidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var logger = new Mock<ILogger<ChargeCommandsHandler>>();
                var integrationService = new Mock<IAcquirerApiService>();
                var command = new Mock<ExpireCharge>(null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(false);
                var handler = new Mock<ChargeCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };
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
                var integrationService = new Mock<IAcquirerApiService>();
                var logger = new Mock<ILogger<ChargeCommandsHandler>>();
                var command = new Mock<ExpireCharge>(null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                eventStore.Setup(x => x.GetByIdAsync<Charge>(command.Object.AggregateKey)).Returns(Task.FromResult(app.Object));
                var handler = new Mock<ChargeCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                app.Verify(x => x.Expire(command.Object.CorrelationKey, command.Object.ApplicationKey, command.Object.SagaProcessKey), Times.Once);
                eventStore.Verify(x => x.SaveAsync(app.Object), Times.Once);
            }
        }

        public class HandleVerifyAcquirerSettlementTests
        {
            [Fact]
            public async void HandeInvalidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var logger = new Mock<ILogger<ChargeCommandsHandler>>();
                var integrationService = new Mock<IAcquirerApiService>();
                var command = new Mock<VerifyAcquirerSettlement>(null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(false);
                var handler = new Mock<ChargeCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };
                await handler.Object.Handle(command.Object, CancellationToken.None);
                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<AggregateRoot>()), Times.Never);
            }

            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var app = new Mock<Charge>();
                var logger = new Mock<ILogger<ChargeCommandsHandler>>();
                var bus = new Mock<IMessageBus>();
                var integrationService = new Mock<IAcquirerApiService>();
                var command = new Mock<VerifyAcquirerSettlement>(null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                eventStore.Setup(x => x.GetByIdAsync<Charge>(command.Object.AggregateKey)).Returns(Task.FromResult(app.Object));
                var handler = new Mock<ChargeCommandsHandler>(eventStore.Object, bus.Object, integrationService.Object, logger.Object) { CallBase = true };

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                app.Verify(x => x.VerifySettlement(command.Object.CorrelationKey, command.Object.ApplicationKey, command.Object.SagaProcessKey, integrationService.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(app.Object), Times.Once);
            }
        }
    }
}
