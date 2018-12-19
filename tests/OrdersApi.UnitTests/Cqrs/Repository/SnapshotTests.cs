using System;
using OrdersApi.Cqrs.Repository;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Repository
{
    public class SnapshotTests
    { 
        [Fact]
        public void ConstructorTest()
        {
            var id = Guid.NewGuid().ToString();
            var aggregateId = Guid.NewGuid().ToString();
            short version = 1;
            var snapshot = new Snapshot(id, aggregateId, version);
            Assert.Equal(id, snapshot.SnapshotKey);
            Assert.Equal(aggregateId, snapshot.AggregateKey);
            Assert.Equal(version, snapshot.Version); 

        }
    }
}
