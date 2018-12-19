using AutoFixture;
using OrdersApi.Domain.Events.ClientApplication;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using OrdersApi.Domain.Model.Projections;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Model
{
    public class ClientApplicationProjectionTests
    {
        [Fact]
        public void CreateProjectionFromEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ClientApplicationCreated>();

            var projection = new ClientApplicationProjection(@event);

            Assert.Equal(@event.ExternalKey, projection.ExternalKey);
            Assert.Equal(@event.AggregateKey, projection.AggregateKey);
            Assert.Equal(@event.EventCommittedTimestamp, projection.CreatedDate);
            Assert.Equal(@event.Name, projection.Name);
            Assert.Equal(@event.TargetVersion + 1, projection.Version);
            Assert.Equal(ClientApplicationStatus.Accepted, projection.Status);
            Assert.False(string.IsNullOrWhiteSpace(projection.Id));
        }

        [Fact]
        public void UpdateProjectionFromClientApplicationActivatedEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ClientApplicationActivated>();
            var projection = fixture.Create<ClientApplicationProjection>();

            projection.Update(@event);   

            Assert.Equal(ClientApplicationStatus.Active, projection.Status);
            Assert.Equal(@event.TargetVersion + 1, projection.Version);
        }

        [Fact]
        public void UpdateProjectionFromClientApplicationCreationRevokedEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ClientApplicationCreationRevoked>();
            var projection = fixture.Create<ClientApplicationProjection>();

            projection.Update(@event);

            Assert.Equal(ClientApplicationStatus.Rejected, projection.Status);
            Assert.Equal(@event.Reason, projection.RejectionReason);
            Assert.Equal(@event.TargetVersion + 1, projection.Version);
        }


        [Fact]
        public void UpdateProjectionFromProductAccesUpdatedEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ProductAccessUpdated>();
            var projection = new Mock<ClientApplicationProjection>() {CallBase = true};

            projection.Object.Update(@event);
             
            projection.Verify(x=> x.Products, Times.Once);
            Assert.Equal(@event.TargetVersion + 1, projection.Object.Version);
        }
    }
}
