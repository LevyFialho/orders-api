using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Specifications;
using OrdersApi.Infrastructure.Settings;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace OrdersApi.Infrastructure.StorageProviders.RavenDb
{

    public class RavenDbRepository<T> : IQueryableRepository<T> where T : Projection
    {
        private readonly IDocumentStore _store;
        private readonly RavenDbSettings _settings;
        private readonly SessionOptions _sessionOptions;
        private bool _disposed;

        public RavenDbRepository(RavenDbSettings settings)
        {
            _settings = settings;
            _sessionOptions = new SessionOptions()
            {
                Database = _settings.DatabaseName,
                NoCaching = _settings.NoCaching,
                NoTracking = _settings.NoCaching,
                TransactionMode = _settings.TransactionMode
            };
            _store = new DocumentStore()
            {
                Database = _settings.DatabaseName,
                Urls = _settings.Urls,

            };
        }

        [ExcludeFromCodeCoverage]
        public void Dispose()
        {

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [ExcludeFromCodeCoverage]
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) { return; }
            if (disposing)
            {
                _store.Dispose();
            }
            _disposed = true;
        }


        protected IAsyncDocumentSession OpenAsyncSession()
        {
            var session = _store.OpenAsyncSession(_sessionOptions);
            if (_settings.UseOptimisticConcurency.HasValue)
                session.Advanced.UseOptimisticConcurrency = _settings.UseOptimisticConcurency.Value;
            if (_settings.MaxNumberOfRequestsPerSession.HasValue)
                session.Advanced.MaxNumberOfRequestsPerSession = _settings.MaxNumberOfRequestsPerSession.Value;
            return session;
        }

        protected IDocumentSession OpenSession()
        {
            var session = _store.OpenSession(_sessionOptions);
            if (_settings.UseOptimisticConcurency.HasValue)
                session.Advanced.UseOptimisticConcurrency = _settings.UseOptimisticConcurency.Value;
            if (_settings.MaxNumberOfRequestsPerSession.HasValue)
                session.Advanced.MaxNumberOfRequestsPerSession = _settings.MaxNumberOfRequestsPerSession.Value;
            return session;
        }

        public T Get(string id)
        {
            using (var session = OpenSession())
            {
                return session.Query<T>().FirstOrDefault(x => x.AggregateKey == id);
            }
        }

        public async Task<T> GetAsync(string id)
        {
            using (var session = OpenAsyncSession())
            {
                return await session.Query<T>().FirstOrDefaultAsync(x => x.AggregateKey == id);
            }
        }

        public IEnumerable<T> GetAll()
        {
            using (var session = OpenSession())
            {
                return session.Query<T>().Where(x => true);
            }
        }

        public IEnumerable<T> GetFiltered(ISpecification<T> specification)
        {
            using (var session = OpenSession())
            {
                return session.Query<T>().Where(specification.SatisfiedBy());
            }
        }

        public Task<IEnumerable<T>> GetFilteredAsync(ISpecification<T> specification)
        {
            return Task.FromResult(GetFiltered(specification));

        }

        public PagedResult<T> GetPaged(ISpecification<T> specification, int pageSize, int pageNumber)
        {
            using (var session = OpenSession())
            {
                var data = session.Query<T>().Statistics(out QueryStatistics stats).Where(specification.SatisfiedBy()).Skip((pageNumber - 1) * pageSize).Take(pageSize);
                return new PagedResult<T>()
                {
                    TotalItems = stats.TotalResults,
                    Items = data,
                    PageSize = pageSize,
                    CurrentPage = pageNumber
                };
            }
        }

        public Task<PagedResult<T>> GetPagedAsync(ISpecification<T> specification, int pageSize, int pageNumber)
        {
            return Task.FromResult(GetPaged(specification, pageSize, pageNumber));
        }

        public SeekResult<T> Seek(ISpecification<T> specification, int limit, string currentIndexOffset, SortDirection sortDirection)
        {
            using (var session = OpenSession())
            {

                var where = specification.GetOffsetSpecification(currentIndexOffset, sortDirection);

                var list = sortDirection == SortDirection.Asc ? session.Query<T>().Where(where.SatisfiedBy()).OrderBy(x => x.AggregateKey).Take(limit).ToList() :
                    session.Query<T>().Where(where.SatisfiedBy()).OrderByDescending(x => x.AggregateKey).Take(limit).ToList();

                return new SeekResult<T>()
                {
                    Items = list,
                    PageSize = limit,
                    CurrentIndexOffset = currentIndexOffset, 
                    NextIndexOffset = list?.LastOrDefault()?.AggregateKey
                };
            }
        }

        public Task<SeekResult<T>> SeekAsync(ISpecification<T> specification, int limit, string currentIndexOffset, SortDirection sortDirection)
        {
            return Task.FromResult(Seek(specification, limit, currentIndexOffset, sortDirection));
        }

        public Task<IEnumerable<T>> GetAllAsync()
        {
            return Task.FromResult(GetAll());
        }

        public void Add(T obj)
        {
            using (var session = OpenSession())
            {
                session.Store(obj, obj.AggregateKey);
            }
        }

        public async Task AddAynsc(T obj)
        {
            using (var session = OpenAsyncSession())
            {
                await session.StoreAsync(obj, obj.AggregateKey);
            }
        }

        public void Update(T obj)
        {
            Add(obj);
        }

        public async Task UpdateAsync(T obj)
        {
            await AddAynsc(obj);
        }

        public void Remove(string id)
        {
            using (var session = OpenSession())
            {
                var user = session.Query<T>().FirstOrDefault(x => x.AggregateKey == id);
                session.Delete(user);
            }
        }

        public async Task RemoveAsync(string id)
        {
            using (var session = OpenAsyncSession())
            {
                var user = await session.Query<T>().FirstOrDefaultAsync(x => x.AggregateKey == id);
                session.Delete(user);
            }
        }

        public long Count(ISpecification<T> specification)
        {
            using (var session = OpenSession())
            {
                return session.Query<T>().Where(specification.SatisfiedBy()).Count(); 
            }
        }

        public IEnumerable<T> GetFilteredSortByDescending(ISpecification<T> specification, Expression<Func<T, object>> sort, int? limit)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> GetFilteredSortByDescendingAsync(ISpecification<T> specification, Expression<Func<T, object>> sort, int? limit)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetFilteredSortBy(ISpecification<T> specification, Expression<Func<T, object>> sort, int? limit)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> GetFilteredSortByAsync(ISpecification<T> specification, Expression<Func<T, object>> sort, int? limit)
        {
            throw new NotImplementedException();
        }
    }
}
