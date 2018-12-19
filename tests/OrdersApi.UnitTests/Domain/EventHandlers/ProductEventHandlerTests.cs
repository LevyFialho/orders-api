using System.Collections.Generic;
using System.Threading;
using AutoFixture;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Commands.Product;
using OrdersApi.Domain.EventHandlers;
using OrdersApi.Domain.Events.Product;
using OrdersApi.Domain.Model.Projections;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Domain.EventHandlers
{
    public class ProductEventHandlerTests
    {
        public class HandleCreatedEventTests
        { 
            [Fact]
            public async void HandleDuplicate()
            {
                var fixture = new Fixture();
                var repository = new Mock<IQueryableRepository<ProductProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = fixture.Create<ProductCreated>();
                var existingClientAppList = new List<ProductProjection>(){new ProductProjection()};
                //Setup existing aggregate
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(default(ProductProjection));
                //Setup existing app
                repository.Setup(x => x.GetFiltered(It.IsAny<ISpecification<ProductProjection>>()))
                    .Returns(existingClientAppList);

                var handler = new Mock<ProductEventsHandler>(repository.Object, bus.Object) { CallBase = true };
                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.GetFiltered(It.IsAny<ISpecification<ProductProjection>>()), Times.Once);
                repository.Verify(x => x.AddAynsc(It.IsAny<ProductProjection>()), Times.Never);
                bus.Verify(x => x.RunNow(It.IsAny<RevokeProductCreation>()), Times.Once);
            }

            [Fact]
            public async void HandleExistingAggregate()
            {
                var fixture = new Fixture();
                var repository = new Mock<IQueryableRepository<ProductProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = fixture.Create<ProductCreated>();
                var existingClientAppList = new List<ProductProjection>();
                //Setup existing aggregate
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(new ProductProjection());
               

                var handler = new Mock<ProductEventsHandler>(repository.Object, bus.Object) { CallBase = true };
                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.GetFiltered(It.IsAny<ISpecification<ProductProjection>>()), Times.Never);
                repository.Verify(x => x.AddAynsc(It.IsAny<ProductProjection>()), Times.Never);
                bus.Verify(x => x.RunNow(It.IsAny<ActivateProduct>()), Times.Never);
                bus.Verify(x => x.RunNow(It.IsAny<RevokeProductCreation>()), Times.Never);
            }

            [Fact]
            public async void HandleNewApplication()
            {
                var fixture = new Fixture();
                var repository = new Mock<IQueryableRepository<ProductProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = fixture.Create<ProductCreated>();
                var existingClientAppList = new List<ProductProjection>();
                //Setup existing aggregate
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(default(ProductProjection));
                //Setup existing app
                repository.Setup(x => x.GetFiltered(It.IsAny<ISpecification<ProductProjection>>()))
                    .Returns(existingClientAppList);

                var handler = new Mock<ProductEventsHandler>(repository.Object, bus.Object){ CallBase = true};
                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.GetFiltered(It.IsAny<ISpecification<ProductProjection>>()), Times.Once);
                repository.Verify(x => x.AddAynsc(It.IsAny<ProductProjection>()), Times.Once);
                bus.Verify(x => x.RunNow(It.IsAny<ActivateProduct>()), Times.Once);
            }
             
        }

        public class HandleActivatedEventTests
        { 
            [Fact]
            public async void Handle()
            {
                var repository = new Mock<IQueryableRepository<ProductProjection>>();
                var app = new Mock<ProductProjection>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = new Mock<ProductActivated>(null, null, null, null, null);
                repository.Setup(x => x.Get(eventData.Object.AggregateKey)).Returns(app.Object);
                var handler = new Mock<ProductEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData.Object, CancellationToken.None);

                repository.Verify(x => x.UpdateAsync(app.Object), Times.Once);
                app.Verify(x => x.Update(eventData.Object), Times.Once());
            }

            [Fact]
            public async void HandeInexistingApp()
            {
                var repository = new Mock<IQueryableRepository<ProductProjection>>(); 
                 var bus = new Mock<ICommandScheduler>();
                var eventData = new Mock<ProductActivated>(null, null, null, null, null);
                repository.Setup(x => x.Get(eventData.Object.AggregateKey)).Returns(default(ProductProjection));
                var handler = new Mock<ProductEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData.Object, CancellationToken.None);
                repository.Verify(x => x.UpdateAsync(It.IsAny<ProductProjection>()), Times.Never); 
            }
        }

        public class HandleRejectedEventTests
        {
            [Fact]
            public async void Handle()
            {
                var repository = new Mock<IQueryableRepository<ProductProjection>>();
                var app = new Mock<ProductProjection>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = new Mock<ProductCreationRevoked>(null, null, null, null, null, null);
                eventData.Object.Reason = "X";
                repository.Setup(x => x.Get(eventData.Object.AggregateKey)).Returns(app.Object);
                var handler = new Mock<ProductEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData.Object, CancellationToken.None);

                repository.Verify(x => x.UpdateAsync(app.Object), Times.Once);
                app.Verify(x => x.Update(eventData.Object), Times.Once());
            }

            [Fact]
            public async void HandeInexistingApp()
            {
                var repository = new Mock<IQueryableRepository<ProductProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = new Mock<ProductCreationRevoked>(null, null, null, null, null, null);
                repository.Setup(x => x.Get(eventData.Object.AggregateKey)).Returns(default(ProductProjection));
                var handler = new Mock<ProductEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData.Object, CancellationToken.None);
                repository.Verify(x => x.UpdateAsync(It.IsAny<ProductProjection>()), Times.Never);
            }
        }

        public class HandleProductAcquirerConfigurationModifiedEventTests
        {
            [Fact]
            public async void Handle()
            {
                var repository = new Mock<IQueryableRepository<ProductProjection>>();
                var app = new Mock<ProductProjection>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = new Mock<ProductAcquirerConfigurationUpdated>(null, null, null, null, null, null);
                repository.Setup(x => x.Get(eventData.Object.AggregateKey)).Returns(app.Object);
                var handler = new Mock<ProductEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData.Object, CancellationToken.None);

                repository.Verify(x => x.UpdateAsync(app.Object), Times.Once);
                app.Verify(x => x.Update(eventData.Object), Times.Once());
            }

            [Fact]
            public async void HandeInexistingApp()
            {
                var repository = new Mock<IQueryableRepository<ProductProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = new Mock<ProductAcquirerConfigurationUpdated>(null, null, null, null, null, null);
                repository.Setup(x => x.Get(eventData.Object.AggregateKey)).Returns(default(ProductProjection));
                var handler = new Mock<ProductEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData.Object, CancellationToken.None);
                repository.Verify(x => x.UpdateAsync(It.IsAny<ProductProjection>()), Times.Never);
            }
        }
    }
}
