using System.Collections.Generic;
using System.Threading;
using AutoFixture;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Commands.ClientApplication;
using OrdersApi.Domain.EventHandlers;
using OrdersApi.Domain.Events.ClientApplication;
using OrdersApi.Domain.Model.Projections;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Domain.EventHandlers
{
    public class ClientApplicationEventHandlerTests
    {
        public class HandleCreatedEventTests
        { 
            [Fact]
            public async void HandleDuplicate()
            {
                var fixture = new Fixture();
                var repository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = fixture.Create<ClientApplicationCreated>();

                var existingClientAppList = new List<ClientApplicationProjection>()
                {
                    new ClientApplicationProjection(),
                    new ClientApplicationProjection()
                };
                
                //Setup existing aggregate
                repository
                    .Setup(x => x.Get(eventData.AggregateKey))
                        .Returns(default(ClientApplicationProjection));
                
                //Setup existing app
                repository
                    .Setup(x => x.GetFiltered(It.IsAny<ISpecification<ClientApplicationProjection>>()))
                        .Returns(existingClientAppList);

                var handler = new Mock<ClientApplicationEventsHandler>(repository.Object, bus.Object) { CallBase = true };
                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Never);
                repository.Verify(x => x.GetFiltered(It.IsAny<ISpecification<ClientApplicationProjection>>()), Times.Once);
                repository.Verify(x => x.AddAynsc(It.IsAny<ClientApplicationProjection>()), Times.Never);
                bus.Verify(x => x.RunNow(It.IsAny<RevokeClientApplicationCreation>()), Times.Once);
            }

            [Fact]
            public async void HandleExistingAggregate()
            {
                var fixture = new Fixture();
                var repository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = fixture.Create<ClientApplicationCreated>();
                var existingClientAppList = new List<ClientApplicationProjection>();
                
                //Setup existing aggregate
                repository
                    .Setup(x => x.Get(eventData.AggregateKey))
                        .Returns(new ClientApplicationProjection());
               

                var handler = new Mock<ClientApplicationEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.GetFiltered(It.IsAny<ISpecification<ClientApplicationProjection>>()), Times.Once);
                repository.Verify(x => x.AddAynsc(It.IsAny<ClientApplicationProjection>()), Times.Never);
                bus.Verify(x => x.RunNow(It.IsAny<ActivateClientApplication>()), Times.Once);
                bus.Verify(x => x.RunNow(It.IsAny<RevokeClientApplicationCreation>()), Times.Never);
            }

            [Fact]
            public async void HandleNewApplication()
            {
                var fixture = new Fixture();
                var repository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = fixture.Create<ClientApplicationCreated>();
                var existingClientAppList = new List<ClientApplicationProjection>();
                //Setup existing aggregate
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(default(ClientApplicationProjection));
                //Setup existing app
                repository.Setup(x => x.GetFiltered(It.IsAny<ISpecification<ClientApplicationProjection>>()))
                    .Returns(existingClientAppList);

                var handler = new Mock<ClientApplicationEventsHandler>(repository.Object, bus.Object){ CallBase = true};
                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.GetFiltered(It.IsAny<ISpecification<ClientApplicationProjection>>()), Times.Once);
                repository.Verify(x => x.AddAynsc(It.IsAny<ClientApplicationProjection>()), Times.Once);
                bus.Verify(x => x.RunNow(It.IsAny<ActivateClientApplication>()), Times.Once);
            }
             
        }

        public class HandleActivatedEventTests
        { 
            [Fact]
            public async void Handle()
            {
                var repository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var app = new Mock<ClientApplicationProjection>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = new Mock<ClientApplicationActivated>(null, null, null, null, null);
                repository.Setup(x => x.Get(eventData.Object.AggregateKey)).Returns(app.Object);
                var handler = new Mock<ClientApplicationEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData.Object, CancellationToken.None);

                repository.Verify(x => x.UpdateAsync(app.Object), Times.Once);
                app.Verify(x => x.Update(eventData.Object), Times.Once());
            }

            [Fact]
            public async void HandeInexistingApp()
            {
                var repository = new Mock<IQueryableRepository<ClientApplicationProjection>>(); 
                 var bus = new Mock<ICommandScheduler>();
                var eventData = new Mock<ClientApplicationActivated>(null, null, null, null, null);
                repository.Setup(x => x.Get(eventData.Object.AggregateKey)).Returns(default(ClientApplicationProjection));
                var handler = new Mock<ClientApplicationEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData.Object, CancellationToken.None);
                repository.Verify(x => x.UpdateAsync(It.IsAny<ClientApplicationProjection>()), Times.Never); 
            }
        }

        public class HandleRejectedEventTests
        {
            [Fact]
            public async void Handle()
            {
                var repository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var app = new Mock<ClientApplicationProjection>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = new Mock<ClientApplicationCreationRevoked>(null, null, null, null, null, null);
                eventData.Object.Reason = "X";
                repository.Setup(x => x.Get(eventData.Object.AggregateKey)).Returns(app.Object);
                var handler = new Mock<ClientApplicationEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData.Object, CancellationToken.None);

                repository.Verify(x => x.UpdateAsync(app.Object), Times.Once);
                app.Verify(x => x.Update(eventData.Object), Times.Once());
            }

            [Fact]
            public async void HandeInexistingApp()
            {
                var repository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = new Mock<ClientApplicationCreationRevoked>(null, null, null, null, null, null);
                repository.Setup(x => x.Get(eventData.Object.AggregateKey)).Returns(default(ClientApplicationProjection));
                var handler = new Mock<ClientApplicationEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData.Object, CancellationToken.None);
                repository.Verify(x => x.UpdateAsync(It.IsAny<ClientApplicationProjection>()), Times.Never);
            }
        }


        public class HandleProductAccessUpdatedEventTests
        {
            [Fact]
            public async void Handle()
            {
                var repository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var app = new Mock<ClientApplicationProjection>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = new Mock<ProductAccessUpdated>(null, null, null, null, null, null);
               
                repository.Setup(x => x.Get(eventData.Object.AggregateKey)).Returns(app.Object);
                var handler = new Mock<ClientApplicationEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData.Object, CancellationToken.None);

                repository.Verify(x => x.UpdateAsync(app.Object), Times.Once);
                app.Verify(x => x.Update(eventData.Object), Times.Once());
            }

            [Fact]
            public async void HandeInexistingApp()
            {
                var repository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var eventData = new Mock<ProductAccessUpdated>(null, null, null, null, null, null);
                repository.Setup(x => x.Get(eventData.Object.AggregateKey)).Returns(default(ClientApplicationProjection));
                var handler = new Mock<ClientApplicationEventsHandler>(repository.Object, bus.Object) { CallBase = true };

                await handler.Object.Handle(eventData.Object, CancellationToken.None);
                repository.Verify(x => x.UpdateAsync(It.IsAny<ClientApplicationProjection>()), Times.Never);
            }
        }
    }
}
