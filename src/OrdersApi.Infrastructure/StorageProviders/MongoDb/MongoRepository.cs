using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Specifications;
using OrdersApi.Infrastructure.Settings;
using MongoDB.Driver;
using SortDirection = OrdersApi.Cqrs.Queries.SortDirection;

namespace OrdersApi.Infrastructure.StorageProviders.MongoDb
{
    [ExcludeFromCodeCoverage]
    public class MongoRepository<T> : IQueryableRepository<T> where T : Projection
    {
        private readonly IMongoCollection<T> _collection;
        private bool _disposed;
        private readonly int _seekLimit;
        private readonly bool _allowSkip;

        public MongoRepository(IMongoDatabase database, MongoSettings settings)
        {
            _collection = database.GetCollection<T>(typeof(T).ToString());
            _seekLimit = settings?.SeekLimit ?? 5000;
            _allowSkip = settings?.AllowSkip ?? false;
        }

        public void Add(T obj)
        {
            _collection.InsertOne(obj);
        }


        public Task AddAynsc(T obj)
        {
            return _collection.InsertOneAsync(obj);
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
            return _collection.FindSync(p => p.AggregateKey == id).SingleOrDefault();
        }

        public IEnumerable<T> GetAll()
        {
            return _collection.FindSync(x => true).ToList();
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
            return _collection.FindSync(specification.SatisfiedBy()).ToList();
        }

        public Task<IEnumerable<T>> GetFilteredAsync(ISpecification<T> specification)
        {
            return Task.FromResult(GetFiltered(specification));
        }

        public Task<IEnumerable<T>> GetFilteredSortByDescendingAsync(ISpecification<T> specification, Expression<Func<T, object>> sort, int? limit)
        {
            return Task.FromResult(GetFilteredSortBy(specification, sort, limit));
        }

        public IEnumerable<T> GetFilteredSortByDescending(ISpecification<T> specification, Expression<Func<T, object>> sort, int? limit)
        {
            return _collection
                             .Find(specification.SatisfiedBy())
                                 .SortByDescending(sort)
                                     .Limit(limit)
                                         .ToList();
        }

        public Task<IEnumerable<T>> GetFilteredSortByAsync(ISpecification<T> specification, Expression<Func<T, object>> sort, int? limit)
        {
            return Task.FromResult(GetFilteredSortBy(specification, sort, limit));
        }

        public IEnumerable<T> GetFilteredSortBy(ISpecification<T> specification, Expression<Func<T, object>> sort, int? limit)
        {
            return _collection
                            .Find(specification.SatisfiedBy())
                                .SortBy(sort)
                                    .Limit(limit)
                                        .ToList();
        }

        public long Count(ISpecification<T> specification)
        {
            try
            {
                return _collection.CountDocuments(specification.SatisfiedBy());
            }
            catch (MongoDB.Driver.MongoCommandException e)
            {
                throw new QueryExecutionException(e.ToString());
            }
        }

        public Task<long> CountAsync(ISpecification<T> specification)
        {
            return _collection.CountDocumentsAsync(specification.SatisfiedBy());
        }

        public void Remove(string id)
        {
            _collection.DeleteOne(x => ((Entity)x).AggregateKey == id);
        }

        public Task RemoveAsync(string id)
        {
            return _collection.DeleteOneAsync(x => ((Entity)x).AggregateKey == id);
        }


        public void Update(T obj)
        {
            _collection.ReplaceOne(x => ((Entity)x).AggregateKey == ((Entity)obj).AggregateKey, obj);
        }

        public Task UpdateAsync(T obj)
        {
            return _collection.ReplaceOneAsync(x => ((Entity)x).AggregateKey == ((Entity)obj).AggregateKey, obj);
        }

        public PagedResult<T> GetPaged(ISpecification<T> specification, int pageSize, int pageNumber)
        {
            var list = new List<T>();

            try
            {
                if (_allowSkip)
                    list.AddRange(_collection.Find(specification.SatisfiedBy()).SortBy(x => x.AggregateKey).Skip((pageNumber - 1) * pageSize).Limit(pageSize)
                        .ToList());
                else
                {
                    int loaded = 0;
                    int total = pageSize * pageNumber;
                    int currentPage = 1;
                    string offset = string.Empty;
                    while (loaded < total)
                    {

                        var filter = GetOffsetFilterDefinition(specification, offset, SortDirection.Asc);

                        var result = _collection.Find(filter).SortBy(x => x.AggregateKey).Limit(pageSize).ToList();

                        if (currentPage == pageNumber)
                            list.AddRange(result);

                        currentPage++;
                        loaded += pageSize;
                        offset = result?.LastOrDefault()?.AggregateKey;
                    }
                }

                return new PagedResult<T>()
                {
                    CurrentPage = pageNumber,
                    Items = list,
                    PageSize = pageSize,
                    TotalItems = Count(specification),
                };
            }
            catch (MongoDB.Driver.MongoCommandException e)
            {
                throw new QueryExecutionException(e.ToString());
            }

        }

        public Task<PagedResult<T>> GetPagedAsync(ISpecification<T> specification, int pageSize, int pageNumber)
        {
            return Task.FromResult(GetPaged(specification, pageSize, pageNumber));
        }

        public SeekResult<T> Seek(ISpecification<T> specification, int limit, string currentIndexOffset, SortDirection sortDirection)
        {
            var result = new List<T>();
            var taken = 0;
            while (taken < limit)
            {
                var filter = GetOffsetFilterDefinition(specification, currentIndexOffset, sortDirection);

                int pageSize = limit < _seekLimit ? limit : _seekLimit;
                var list = sortDirection == SortDirection.Asc ? _collection.Find(filter).SortBy(x => x.AggregateKey).Limit(pageSize).ToList() :
                    _collection.Find(filter).SortByDescending(x => x.AggregateKey).Limit(pageSize).ToList();
                result.AddRange(list);
                taken += pageSize;
            }

            return new SeekResult<T>()
            {
                Items = result,
                NextIndexOffset = result?.LastOrDefault()?.AggregateKey,
                CurrentIndexOffset = currentIndexOffset,
                PageSize = limit,
            };
        }

        private FilterDefinition<TC> GetOffsetFilterDefinition<TC>(ISpecification<TC> specification, string currentIndexOffset, SortDirection sortDirection) where TC : Projection
        {
            if (string.IsNullOrWhiteSpace(currentIndexOffset))
                return Builders<TC>.Filter.Where(specification.SatisfiedBy());
            return (sortDirection == SortDirection.Asc
                       ? Builders<TC>.Filter.Gt(x => x.AggregateKey, currentIndexOffset)
                       : Builders<TC>.Filter.Lt(x => x.AggregateKey, currentIndexOffset)) &
                   Builders<TC>.Filter.Where(specification.SatisfiedBy());
        }

        public Task<SeekResult<T>> SeekAsync(ISpecification<T> specification, int limit, string currentIndexOffset, SortDirection sortDirection)
        {
            return Task.FromResult(Seek(specification, limit, currentIndexOffset, sortDirection));
        }
    }
}
