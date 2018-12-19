using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.CommandHandlers;
using OrdersApi.Domain.Commands.ClientApplication;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using OrdersApi.Domain.Model.ProductAggregate;
using FluentAssertions;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Domain.CommandHandlers
{
    public class ClientApplicationCommandHandlerTests
    {
        public class HandleCreateClientApplicationCommandTests
        {
            [Fact]
            public async void HandeInvalidCommand()
            { 
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var command = new Mock<CreateClientApplication>(null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(false);
                var handler = new Mock<ClientApplicationCommandsHandler>(eventStore.Object, bus.Object){ CallBase = true};
                await handler.Object.Handle(command.Object, CancellationToken.None);
                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<AggregateRoot>()), Times.Never);
            }

            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var command = new Mock<CreateClientApplication>(null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                var handler = new Mock<ClientApplicationCommandsHandler>(eventStore.Object, bus.Object) { CallBase = true };
                await handler.Object.Handle(command.Object, CancellationToken.None);
                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<ClientApplication>()), Times.Once);
            } 
        }

        public class HandleActivateClientApplicationCommandTests
        { 
            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var app = new Mock<ClientApplication>();
                var bus = new Mock<IMessageBus>();
                var command = new Mock<ActivateClientApplication>(null, null, null, null);
                string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                eventStore.Setup(x => x.GetByIdAsync<ClientApplication>(command.Object.AggregateKey)).Returns(Task.FromResult(app.Object));
                var handler = new Mock<ClientApplicationCommandsHandler>(eventStore.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                app.Verify(x => x.Activate(command.Object.CorrelationKey, command.Object.ApplicationKey, command.Object.SagaProcessKey), Times.Once);
                eventStore.Verify(x => x.SaveAsync(app.Object), Times.Once);
            }
        }

        public class HandleRejectClientApplicationCommandTests
        {
            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var app = new Mock<ClientApplication>();
                var bus = new Mock<IMessageBus>();
                var command = new Mock<RevokeClientApplicationCreation>(null, null, null, null, null);
                eventStore.Setup(x => x.GetByIdAsync<ClientApplication>(command.Object.AggregateKey)).Returns(Task.FromResult(app.Object));
                var handler = new Mock<ClientApplicationCommandsHandler>(eventStore.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                app.Verify(x => x.Reject(command.Object.CorrelationKey, command.Object.ApplicationKey, command.Object.Reason, command.Object.SagaProcessKey), Times.Once);
                eventStore.Verify(x => x.SaveAsync(app.Object), Times.Once);
            }
        }

        public class HandleUpdateProductAccessCommandTests
        {
            [Fact]
            public void HandleInvalidProduct()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var mockClient = new Mock<ClientApplication>();
                var mockProduct = new Mock<Product>();
                mockClient.Setup(x => x.Status).Returns(ClientApplicationStatus.Active);
                mockProduct.Setup(x => x.Status).Returns(ProductStatus.Rejected);
                var command = new Mock<UpdateProductAccess>(null, null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                eventStore.Setup(x => x.GetByIdAsync<ClientApplication>(It.IsAny<string>())).Returns(Task.FromResult(mockClient.Object));
                eventStore.Setup(x => x.GetByIdAsync<Product>(It.IsAny<string>())).Returns(Task.FromResult(mockProduct.Object));

                var handler = new Mock<ClientApplicationCommandsHandler>(eventStore.Object, bus.Object) { CallBase = true };
                Func<Task> invocation = async () => await handler.Object.Handle(command.Object, CancellationToken.None); 
                invocation.Should().Throw<AggregateNotFoundException>();

                eventStore.Verify(x => x.GetByIdAsync<ClientApplication>(It.IsAny<string>()), Times.Once);
                eventStore.Verify(x => x.GetByIdAsync<Product>(It.IsAny<string>()), Times.Once);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<ClientApplication>()), Times.Never);
            }

            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var mockClient = new Mock<ClientApplication>();
                var mockProduct = new Mock<Product>();
                mockClient.Setup(x => x.Status).Returns(ClientApplicationStatus.Active);
                mockProduct.Setup(x => x.Status).Returns(ProductStatus.Active);
                var command = new Mock<UpdateProductAccess>(null, null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                eventStore.Setup(x => x.GetByIdAsync<ClientApplication>(It.IsAny<string>())).Returns(Task.FromResult(mockClient.Object));
                eventStore.Setup(x => x.GetByIdAsync<Product>(It.IsAny<string>())).Returns(Task.FromResult(mockProduct.Object));

                var handler = new Mock<ClientApplicationCommandsHandler>(eventStore.Object, bus.Object) { CallBase = true };
                await handler.Object.Handle(command.Object, CancellationToken.None);

                eventStore.Verify(x => x.GetByIdAsync<ClientApplication>(It.IsAny<string>()), Times.Once);
                eventStore.Verify(x => x.GetByIdAsync<Product>(It.IsAny<string>()), Times.Once);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<ClientApplication>()), Times.Once);
            }
        }
    }
}
