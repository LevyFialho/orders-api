using System;
using System.Linq;
using AutoFixture;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Events.Product;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Domain.Model.Snapshots;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Model
{
    public class ProductTests
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
            var app = new Mock<Product>(aggregateKey, correlationKey, externalKey, applicationKey, name, sagaProcessKey) {CallBase = true};
            var constructed = app.Object;
            app.Verify(x => x.OnCreated(It.IsAny<ProductCreated>()), Times.Once);
        }

        [Fact]
        public void ActivateAppliesActivationEventTest()
        {
            var correlationKey = Guid.NewGuid().ToString("N");
            var applicationKey = IdentityGenerator.NewSequentialIdentity();
            var app = new Mock<Product>() { CallBase = true };
            var sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
            app.Object.Activate(correlationKey, applicationKey, sagaProcessKey);
            app.Verify(x => x.OnActivated(It.IsAny<ProductActivated>()), Times.Once);
        }

        [Fact]
        public void RejectAppliesRejectionEventTest()
        {
            var correlationKey = Guid.NewGuid().ToString("N");
            var applicationKey = IdentityGenerator.NewSequentialIdentity();
            var sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
            var reason = "X";
            var app = new Mock<Product>() { CallBase = true };
            app.Object.RevokeCreation(correlationKey, applicationKey, reason, sagaProcessKey);
            app.Verify(x => x.OnCreationRevoked(It.IsAny<ProductCreationRevoked>()), Times.Once);
        }

        [Fact]
        public void SetAcquirerConfigurationTest()
        {
            var correlationKey = Guid.NewGuid().ToString("N");
            var fixture = new Fixture();
            var configData = fixture.Create<AcquirerConfiguration>();
            var applicationKey = IdentityGenerator.NewSequentialIdentity();
            var sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
            var app = new Mock<Product>() { CallBase = true };
            app.Object.SetAcquirerConfiguration(correlationKey, applicationKey, sagaProcessKey, configData);
            app.Verify(x => x.OnAcquirerConfigurationModified(It.IsAny<ProductAcquirerConfigurationUpdated>()), Times.Once);
        }

        [Fact]
        public void OnCreatedTest()
        {
            var fixture = new Fixture(); 
            var app = new Product();
            var eventData = fixture.Create<ProductCreated>();
            app.OnCreated(eventData);
            Assert.Equal(eventData.EventCommittedTimestamp, app.CreatedDate);
            Assert.Equal(eventData.ExternalKey, app.ExternalKey);
            Assert.Equal(eventData.Name, app.Name);
            Assert.Equal(eventData.AggregateKey, app.AggregateKey);
            Assert.Equal(ProductStatus.Accepted, app.Status); 

        }

        [Fact]
        public void OnActivatedTest()
        {
            var fixture = new Fixture();
            var app = new Product();
            var eventData = fixture.Create<ProductActivated>();
            app.OnActivated(eventData); 
            Assert.Equal(ProductStatus.Active, app.Status);
        }

        [Fact]
        public void OnCreationRevokedTest()
        {
            var fixture = new Fixture();
            var app = new Product();
            var eventData = fixture.Create<ProductCreationRevoked>();
            app.OnCreationRevoked(eventData);
            Assert.Equal(ProductStatus.Rejected, app.Status);
        }

        [Fact]
        public void OnAcquirerConfigurationModifiedTest()
        {
            var fixture = new Fixture();
            var app = new Product();
            var configData = fixture.Create<AcquirerConfiguration>();
            var eventData = fixture.Create<ProductAcquirerConfigurationUpdated>();
            eventData.Configuration = configData;
            app.OnAcquirerConfigurationModified(eventData);
            Assert.Single(app.AcquirerConfigurations);
            Assert.Equal(configData.AcquirerKey, app.AcquirerConfigurations.First().AcquirerKey);
            Assert.Equal(configData.AccountKey, app.AcquirerConfigurations.First().AccountKey);
            Assert.Equal(configData.CanCharge, app.AcquirerConfigurations.First().CanCharge);
        }

        [Fact]
        public void TakeSnapshotTest()
        {
            var fixture = new Fixture();
            var app = fixture.Create<Product>();
            var snap = app.TakeSnapshot() as ProductSnapshot;
            Assert.NotNull(snap);
            Assert.Equal(app.AggregateKey, snap.AggregateKey);
            Assert.False(string.IsNullOrWhiteSpace(snap.SnapshotKey));
            Assert.Equal(app.ExternalKey, snap.ExternalKey); 
            Assert.Equal(app.CreatedDate, snap.CreatedDate);
            Assert.Equal(app.CurrentVersion, snap.Version);
            Assert.Equal(app.Status, snap.Status);
            Assert.Equal(app.Name, snap.Name);
            Assert.Equal(app.AcquirerConfigurations.Count, snap.AcquirerConfigurations.Count);
        }

        [Fact]
        public void ApplySnapshotTest()
        {
            var fixture = new Fixture();
            var app = fixture.Create<Product>();
            var snap = fixture.Create<ProductSnapshot>();
            app.ApplySnapshot(snap);
            Assert.Equal(snap.ExternalKey, app.ExternalKey);  
            Assert.Equal(snap.CreatedDate, app.CreatedDate);
            Assert.Equal(snap.Version, app.CurrentVersion);
            Assert.Equal(snap.Status, app.Status);
            Assert.Equal(snap.Name, app.Name);
            Assert.Equal(snap.AcquirerConfigurations.Count, app.AcquirerConfigurations.Count);
        } 
    }
}
