using System;
using System.Linq;
using AutoFixture;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Events.ClientApplication;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using OrdersApi.Domain.Model.Snapshots;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Model
{
    public class ClientApplicationTests
    {
        [Fact]
        public void ConstructorAppliesCreatedEventTest()
        {
            var name = "Name";
            var correlationKey = Guid.NewGuid().ToString("N");
            var aggregateKey = IdentityGenerator.NewSequentialIdentity();
            var applicationKey = IdentityGenerator.NewSequentialIdentity();
            var sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
            var externalKey = Guid.NewGuid().ToString("N");
            var app = new Mock<ClientApplication>(aggregateKey, correlationKey, externalKey, applicationKey, name, sagaProcessKey) {CallBase = true};
            var constructed = app.Object;
            app.Verify(x => x.OnCreated(It.IsAny<ClientApplicationCreated>()), Times.Once);
        }

        [Fact]
        public void ActivateAppliesActivationEventTest()
        {
            var correlationKey = Guid.NewGuid().ToString("N");
            var applicationKey = IdentityGenerator.NewSequentialIdentity();
            var sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
            var app = new Mock<ClientApplication>() { CallBase = true };
            app.Object.Activate(correlationKey, applicationKey, sagaProcessKey);
            app.Verify(x => x.OnActivated(It.IsAny<ClientApplicationActivated>()), Times.Once);
        }

        [Fact]
        public void UpdateProductAccessAppliesProductUpdatedEventTest()
        {
            var correlationKey = Guid.NewGuid().ToString("N");
            var applicationKey = IdentityGenerator.NewSequentialIdentity();
            var sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
            var app = new Mock<ClientApplication>() { CallBase = true };
            app.Object.UpdateProductAccess(correlationKey, applicationKey, new ProductAccess(), sagaProcessKey);
            app.Verify(x => x.OnProductAccessUpdated(It.IsAny<ProductAccessUpdated>()), Times.Once);
        }

        [Fact]
        public void RejectAppliesRejectionEventTest()
        {
            var correlationKey = Guid.NewGuid().ToString("N");
            var applicationKey = IdentityGenerator.NewSequentialIdentity();
            var sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
            var reason = "X";
            var app = new Mock<ClientApplication>() { CallBase = true };
            app.Object.Reject(correlationKey, applicationKey, reason, sagaProcessKey);
            app.Verify(x => x.OnRejected(It.IsAny<ClientApplicationCreationRevoked>()), Times.Once);
        }

        [Fact]
        public void OnCreatedTest()
        {
            var fixture = new Fixture(); 
            var app = new ClientApplication();
            var eventData = fixture.Create<ClientApplicationCreated>();
            app.OnCreated(eventData);
            Assert.Equal(eventData.EventCommittedTimestamp, app.CreatedDate);
            Assert.Equal(eventData.ExternalKey, app.ExternalKey);
            Assert.Equal(eventData.Name, app.Name);
            Assert.Equal(eventData.AggregateKey, app.AggregateKey);
            Assert.Equal(ClientApplicationStatus.Accepted, app.Status); 

        }

        [Fact]
        public void OnActivatedTest()
        {
            var fixture = new Fixture();
            var app = new ClientApplication();
            var eventData = fixture.Create<ClientApplicationActivated>();
            app.OnActivated(eventData); 
            Assert.Equal(ClientApplicationStatus.Active, app.Status);
        }

        [Fact]
        public void OnRejectedTest()
        {
            var fixture = new Fixture();
            var app = new ClientApplication();
            var eventData = fixture.Create<ClientApplicationCreationRevoked>();
            app.OnRejected(eventData);
            Assert.Equal(ClientApplicationStatus.Rejected, app.Status);
        }

        [Fact]
        public void OnProductAccessUpdatedTest()
        {
            var fixture = new Fixture();
            var app = new ClientApplication();
            var eventData = fixture.Create<ProductAccessUpdated>();
            eventData.ProductAccess.CanCharge = true;
            app.OnProductAccessUpdated(eventData);
            Assert.True(app.Products.First(x => x.ProductAggregateKey == eventData.ProductAccess.ProductAggregateKey).CanCharge);
            eventData.ProductAccess.CanCharge = false;
            eventData.ProductAccess.CanQuery = false;
            app.OnProductAccessUpdated(eventData);
            Assert.Null(app.Products.FirstOrDefault(x => x.ProductAggregateKey == eventData.ProductAccess.ProductAggregateKey));
        }

        [Fact]
        public void TakeSnapshotTest()
        {
            var fixture = new Fixture();
            var app = fixture.Create<ClientApplication>();
            var snap = app.TakeSnapshot() as ClientApplicationSnapshot;
            Assert.NotNull(snap);
            Assert.Equal(app.AggregateKey, snap.AggregateKey);
            Assert.False(string.IsNullOrWhiteSpace(snap.SnapshotKey));
            Assert.Equal(app.ExternalKey, snap.ExternalKey);
            Assert.Equal(app.Products.Count, snap.Products.Count);
            Assert.Equal(app.CreatedDate, snap.CreatedDate);
            Assert.Equal(app.CurrentVersion, snap.Version);
            Assert.Equal(app.Status, snap.Status);
            Assert.Equal(app.Name, snap.Name);
        }

        [Fact]
        public void ApplySnapshotTest()
        {
            var fixture = new Fixture();
            var app = fixture.Create<ClientApplication>();
            var snap = fixture.Create<ClientApplicationSnapshot>();
            app.ApplySnapshot(snap);
            Assert.Equal(snap.ExternalKey, app.ExternalKey); 
            Assert.Equal(snap.Products.Count, app.Products.Count);
            Assert.Equal(snap.CreatedDate, app.CreatedDate);
            Assert.Equal(snap.Version, app.CurrentVersion);
            Assert.Equal(snap.Status, app.Status);
            Assert.Equal(snap.Name, app.Name);
        } 
    }
}
