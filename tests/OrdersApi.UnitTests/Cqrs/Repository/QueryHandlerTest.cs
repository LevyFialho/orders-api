using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Cqrs.Repository;
using OrdersApi.UnitTests.Cqrs.Events;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Repository
{
    public class QueryHandlerTests
    { 
        [Fact]
        public async void HandleSpecificationTest()
        {
            var repository = new Mock<IQueryableRepository<TestProjection>>();
            var snapshotProvider = new Mock<ISnapshotStorageProvider>();
            var spec = new DirectSpecification<TestProjection>(x => true);
            repository.Setup(x => x.GetFilteredAsync(spec)).Verifiable();
            repository.Setup(x => x.GetFilteredAsync(spec)).Returns(Task.FromResult((IEnumerable<TestProjection>)new List<TestProjection>()));
            var handler = new Mock<QueryHandler<TestProjection>>(repository.Object, snapshotProvider.Object);
            await handler.Object.Handle(spec, CancellationToken.None);
            repository.Verify(x => x.GetFilteredAsync(spec), Times.Once);

        }

        [Fact]
        public async void HandlePagedQueryTest()
        {
            var repository = new Mock<IQueryableRepository<TestProjection>>();
            var snapshotProvider = new Mock<ISnapshotStorageProvider>();
            var spec = new PagedQuery<TestProjection>();
            repository.Setup(x => x.GetPagedAsync(spec.Specification, spec.PageSize, spec.PageNumber)).Verifiable();
            repository.Setup(x => x.GetPagedAsync(spec.Specification, spec.PageSize, spec.PageNumber)).Returns(Task.FromResult(new PagedResult<TestProjection>()));
            var handler = new Mock<QueryHandler<TestProjection>>(repository.Object, snapshotProvider.Object);
            await handler.Object.Handle(spec, CancellationToken.None);
            repository.Verify(x => x.GetPagedAsync(spec.Specification, spec.PageSize, spec.PageNumber), Times.Once);

        }
         
        [Fact]
        public async void HandleSnapshotQueryTest()
        { 
            var repository = new Mock<IQueryableRepository<TestProjection>>();
            var snapshotProvider = new Mock<ISnapshotStorageProvider>();
            var spec = new SnapshotQuery<TestProjection>();
            var proj = new TestProjection();
            var list = new List<TestProjection>() {proj};
            repository.Setup(x => x.GetFiltered(spec.Specification)).Verifiable();
            repository.Setup(x => x.GetFiltered(spec.Specification)).Returns(list);
            var handler = new Mock<QueryHandler<TestProjection>>(repository.Object, snapshotProvider.Object);
            await handler.Object.Handle(spec, CancellationToken.None);
            repository.Verify(x => x.GetFiltered(spec.Specification), Times.Once);
            snapshotProvider.Verify(x => x.GetProjectionSnapshotAsync(typeof(TestProjection), spec.SnapshotKey), Times.Once);
            snapshotProvider.Verify(x => x.SaveProjectionSnapshotAsync(proj), Times.Once);
        }
    }
}
