using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Specifications;
using OrdersApi.Infrastructure.Settings;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;

namespace OrdersApi.Infrastructure.StorageProviders.DocumentDb
{
    public class DocumentDbRepository<T> : IQueryableRepository<T> where T : Projection
    {
        private readonly DocumentClient _client;
        private readonly DocumentCollection _collection;

        private bool _disposed;

        public DocumentDbRepository(DocumentClient client, DocumentDbSettings settings)
        {
            _client = client;
            GetOrCreateDatabase(settings.DatabaseName);
            _collection = GetOrCreateCollection(settings.DatabaseName, typeof(T).Name);

        }

        public DocumentCollection GetOrCreateCollection(string databaseLink, string collectionId)
        {
            var col = _client.CreateDocumentCollectionQuery(databaseLink)
            .Where(c => c.Id == collectionId)
            .AsEnumerable()
            .FirstOrDefault();

            if (col == null)
            {
                col = _client.CreateDocumentCollectionAsync(databaseLink,
                    new DocumentCollection { Id = collectionId },
                    new RequestOptions { OfferType = "S1" }).Result;
            }

            return col;
        }
        public Database GetOrCreateDatabase(string databaseId)
        {
            var db = _client.CreateDatabaseQuery()
            .Where(d => d.Id == databaseId)
            .AsEnumerable()
            .FirstOrDefault();

            if (db == null)
            {
                db = _client.CreateDatabaseAsync(new Database { Id = databaseId }).Result;
            }

            return db;
        }

        public void Add(T obj)
        {
            _client.CreateDocumentAsync(_collection.SelfLink, obj).Wait();
        }

        public async Task AddAynsc(T obj)
        {
            await _client.CreateDocumentAsync(_collection.SelfLink, obj);
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

            _disposed = true;
        }

        public T Get(string id)
        {
            T doc = _client.CreateDocumentQuery<T>(_collection.SelfLink)
                .Where(d => d.Id == id)
                .AsEnumerable()
                .FirstOrDefault();

            return doc;
        }

        public IEnumerable<T> GetAll()
        {
            return _client.CreateDocumentQuery<T>(_collection.SelfLink)
                .Where(d => true)
                .AsEnumerable();
        }

        public Task<SeekResult<T>> SeekAsync(ISpecification<T> specification, int limit, string currentIndexOffset, SortDirection sortDirection)
        {
            return Task.FromResult(Seek(specification, limit, currentIndexOffset, sortDirection));
        }

        public Task<IEnumerable<T>> GetAllAsync()
        {
            return Task.FromResult(GetAll());
        }

        public Task<T> GetAsync(string id)
        {
            return Task.FromResult(Get(id));
        }

        public IEnumerable<T> GetFiltered(ISpecification<T> specification)
        {
            return _client.CreateDocumentQuery<T>(_collection.SelfLink)
                .Where(specification.SatisfiedBy())
                .AsEnumerable();
        }

        public Task<IEnumerable<T>> GetFilteredAsync(ISpecification<T> specification)
        {
            return Task.FromResult(GetFiltered(specification));
        }

        public long Count(ISpecification<T> specification)
        {
            return _client.CreateDocumentQuery<T>(_collection.SelfLink)
                .Where(specification.SatisfiedBy())
                .AsEnumerable().Count();
        }

        public Task<long> CountAsync(ISpecification<T> specification)
        {
            return Task.FromResult(Count(specification));
        }

        public void Remove(string id)
        {
            var doc = _client.CreateDocumentQuery(_collection.SelfLink)
                .Where(d => d.Id == id)
                .AsEnumerable()
                .FirstOrDefault();
            if (doc != null)
                _client.DeleteDocumentAsync(doc.SelfLink).Wait();
        }

        public async Task RemoveAsync(string id)
        {
            var doc = _client.CreateDocumentQuery(_collection.SelfLink)
                .Where(d => d.Id == id)
                .AsEnumerable()
                .FirstOrDefault();
            if (doc != null)
                await _client.DeleteDocumentAsync(doc.SelfLink);
        }


        public void Update(T obj)
        {
            var doc = _client.CreateDocumentQuery(_collection.SelfLink)
                .Where(d => d.Id == obj.Id)
                .AsEnumerable()
                .FirstOrDefault();

            if (doc != null)
            {
                _client.ReplaceDocumentAsync(doc.SelfLink, obj).Wait();
            }
        }

        public async Task UpdateAsync(T obj)
        {
            var doc = _client.CreateDocumentQuery(_collection.SelfLink)
                .Where(d => d.Id == obj.Id)
                .AsEnumerable()
                .FirstOrDefault();

            if (doc != null)
            {
                await _client.ReplaceDocumentAsync(doc.SelfLink, obj);
            }
        }

        public PagedResult<T> GetPaged(ISpecification<T> specification, int pageSize, int pageNumber)
        {
            return GetPagedAsync(specification, pageSize, pageNumber).Result;
        }

        public async Task<PagedResult<T>> GetPagedAsync(ISpecification<T> specification, int pageSize, int pageNumber)
        {
            string continuationToken = null;
            FeedResponse<T> feedResponse = null;
            var currentPage = 0;

            do
            {
                currentPage++;

                var feedOptions = new FeedOptions
                {
                    MaxItemCount = pageSize,
                    EnableCrossPartitionQuery = true,
                    RequestContinuation = continuationToken,
                };

                var filter = _client.CreateDocumentQuery<T>(_collection.SelfLink, feedOptions).Where(specification.SatisfiedBy());

                var query = filter.AsDocumentQuery();

                feedResponse = await query.ExecuteNextAsync<T>();
                continuationToken = feedResponse.ResponseContinuation;

            } while (string.IsNullOrEmpty(continuationToken) || (currentPage == pageNumber));

            var documents = new List<T>();

            foreach (var document in feedResponse) documents.Add(document);

            return new PagedResult<T>()
            {
                CurrentPage = pageNumber,
                Items = documents,
                PageSize = pageSize,
                TotalItems = await CountAsync(specification),
            };
        }

        public SeekResult<T> Seek(ISpecification<T> specification, int limit, string currentIndexOffset, SortDirection sortDirection)
        {
            var where = specification.GetOffsetSpecification(currentIndexOffset, sortDirection);

            var list = sortDirection == SortDirection.Asc ? _client.CreateDocumentQuery<T>(_collection.SelfLink)
                .Where(where.SatisfiedBy()).OrderBy(x => x.AggregateKey).Take(limit).ToList() :
                _client.CreateDocumentQuery<T>(_collection.SelfLink)
                    .Where(where.SatisfiedBy()).OrderByDescending(x => x.AggregateKey).Take(limit).ToList();

            return new SeekResult<T>()
            {
                Items = list,
                PageSize = limit,
                CurrentIndexOffset = currentIndexOffset, 
                NextIndexOffset = list?.LastOrDefault()?.AggregateKey
            };
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
