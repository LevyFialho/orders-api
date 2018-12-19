using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.CommandHandlers;
using OrdersApi.Domain.Commands.Product;
using OrdersApi.Domain.Model.ProductAggregate;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Domain.CommandHandlers
{
    public class ProductCommandsHandlerTests
    {
        public class HandleCreateProductCommandCommandTests
        {
            [Fact]
            public async void HandeInvalidCommand()
            { 
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var command = new Mock<CreateProduct>(null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(false);
                var handler = new Mock<ProductCommandsHandler>(eventStore.Object, bus.Object){ CallBase = true};
                await handler.Object.Handle(command.Object, CancellationToken.None);
                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<AggregateRoot>()), Times.Never);
            }

            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var bus = new Mock<IMessageBus>();
                var command = new Mock<CreateProduct>(null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                var handler = new Mock<ProductCommandsHandler>(eventStore.Object, bus.Object) { CallBase = true };
                await handler.Object.Handle(command.Object, CancellationToken.None);
                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                eventStore.Verify(x => x.SaveAsync(It.IsAny<Product>()), Times.Once);
            } 
        }

        public class HandleActivateProductCommandTests
        { 
            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var app = new Mock<Product>();
                var bus = new Mock<IMessageBus>();
                var command = new Mock<ActivateProduct>(null, null, null, null);
                eventStore.Setup(x => x.GetByIdAsync<Product>(command.Object.AggregateKey)).Returns(Task.FromResult(app.Object));
                var handler = new Mock<ProductCommandsHandler>(eventStore.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                app.Verify(x => x.Activate(command.Object.CorrelationKey, command.Object.ApplicationKey, command.Object.SagaProcessKey), Times.Once);
                eventStore.Verify(x => x.SaveAsync(app.Object), Times.Once);
            }
        }

        public class HandleRejectProductCommandTests
        {
            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var app = new Mock<Product>();
                var bus = new Mock<IMessageBus>();
                var command = new Mock<RevokeProductCreation>(null, null, null, null, null);
                eventStore.Setup(x => x.GetByIdAsync<Product>(command.Object.AggregateKey)).Returns(Task.FromResult(app.Object));
                var handler = new Mock<ProductCommandsHandler>(eventStore.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                app.Verify(x => x.RevokeCreation(command.Object.CorrelationKey, command.Object.ApplicationKey, command.Object.Reason, command.Object.SagaProcessKey), Times.Once);
                eventStore.Verify(x => x.SaveAsync(app.Object), Times.Once);
            }
        }

        public class HandleSetProductAcquirerConfigurationCommandTests
        {
            [Fact]
            public async void HandeValidCommand()
            {
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var app = new Mock<Product>();
                var bus = new Mock<IMessageBus>();
                var command = new Mock<UpdateProductAcquirerConfiguration>(null, null, null, null, null, null, null);
                eventStore.Setup(x => x.GetByIdAsync<Product>(command.Object.AggregateKey)).Returns(Task.FromResult(app.Object));
                var handler = new Mock<ProductCommandsHandler>(eventStore.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(command.Object, CancellationToken.None);

                handler.Verify(x => x.NotifyValidationErrors(command.Object), Times.Never);
                handler.Verify(x => x.ValidateDuplicateCommand(command.Object), Times.Once);
                app.Verify(x => x.SetAcquirerConfiguration(command.Object.CorrelationKey, command.Object.ApplicationKey, command.Object.SagaProcessKey, command.Object.Configuration), Times.Once);
                eventStore.Verify(x => x.SaveAsync(app.Object), Times.Once);
            }
        }
    }
}
