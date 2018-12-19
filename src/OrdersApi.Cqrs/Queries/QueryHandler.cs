using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Cqrs.Repository;
using MediatR;

namespace OrdersApi.Cqrs.Queries
{
    public class QueryHandler<TAggregate> : IQueryHandler<ISpecification<TAggregate>, IEnumerable<TAggregate>>, 
        IQueryHandler<PagedQuery<TAggregate>, PagedResult<TAggregate>>, IQueryHandler<SnapshotQuery<TAggregate>, TAggregate>,
        IQueryHandler<SeekQuery<TAggregate>, SeekResult<TAggregate>> where TAggregate : Projection
    {
        protected readonly IQueryableRepository<TAggregate> Repository; 
        protected readonly ISnapshotStorageProvider SnapshotStorage;

        public QueryHandler(IQueryableRepository<TAggregate> repository, ISnapshotStorageProvider snapshotStorage)
        {
            Repository = repository;
            SnapshotStorage = snapshotStorage;
        }

        public Task<IEnumerable<TAggregate>> Handle(ISpecification<TAggregate> request, CancellationToken cancellationToken)
        {
            return Repository.GetFilteredAsync(request);
        }

        public Task<PagedResult<TAggregate>> Handle(PagedQuery<TAggregate> request, CancellationToken cancellationToken)
        {
            return Repository.GetPagedAsync(request.Specification, request.PageSize, request.PageNumber);
        }

        public async Task<TAggregate> Handle(SnapshotQuery<TAggregate> request, CancellationToken cancellationToken)
        {
            var cacheData = await SnapshotStorage.GetProjectionSnapshotAsync(typeof(TAggregate), request.SnapshotKey); //Search Cache
            var projection = cacheData as TAggregate;
            if (cacheData == null)
            {
                projection = Repository.GetFiltered(request.Specification).FirstOrDefault(); //Search DB
                if (projection != null)
                    await SnapshotStorage.SaveProjectionSnapshotAsync(projection);
            }
            return projection;
        }

        public Task<SeekResult<TAggregate>> Handle(SeekQuery<TAggregate> request, CancellationToken cancellationToken)
        {
            return Repository.SeekAsync(request.Specification, request.PageSize, request.IndexOffset,
                request.SortDirection);
        }
    }

    
}
