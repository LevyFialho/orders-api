using System.Linq;
using AutoFixture;
using OrdersApi.Domain.Events.Product;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Domain.Model.Projections;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Model
{
    public class ProductProjectionTests
    {
        [Fact]
        public void CreateProjectionFromEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ProductCreated>();

            var projection = new ProductProjection(@event);

            Assert.Equal(@event.ExternalKey, projection.ExternalKey);
            Assert.Equal(@event.AggregateKey, projection.AggregateKey);
            Assert.Equal(@event.EventCommittedTimestamp, projection.CreatedDate);
            Assert.Equal(@event.Name, projection.Name);
            Assert.Equal(ProductStatus.Accepted, projection.Status);
            Assert.False(string.IsNullOrWhiteSpace(projection.Id));
        }

        [Fact]
        public void UpdateProjectionFromProductActivatedEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ProductActivated>();
            var projection = fixture.Create<ProductProjection>();

            projection.Update(@event);   

            Assert.Equal(ProductStatus.Active, projection.Status);
        }

        [Fact]
        public void UpdateProjectionFromProductCreationRevokedEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ProductCreationRevoked>();
            var projection = fixture.Create<ProductProjection>();

            projection.Update(@event);

            Assert.Equal(ProductStatus.Rejected, projection.Status);
            Assert.Equal(@event.Reason, projection.RejectionReason);
        }


        [Fact]
        public void UpdateProjectionFromProductAcquirerConfigurationModifiedEventTest()
        {
            var fixture = new Fixture();
            var config = fixture.Create<AcquirerConfiguration>();
            var @event = fixture.Create<ProductAcquirerConfigurationUpdated>();
            @event.Configuration = config;
            var projection = fixture.Create<ProductProjection>();

            projection.Update(@event);
            Assert.Single(projection.AcquirerConfigurations.Where(x => x.AcquirerKey == config.AcquirerKey && x.AccountKey == config.AccountKey && x.AccountKey == config.AccountKey));
        }
    }
}
