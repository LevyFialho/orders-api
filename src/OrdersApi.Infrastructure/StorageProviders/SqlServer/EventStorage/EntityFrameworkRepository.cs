using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using Microsoft.EntityFrameworkCore;

namespace OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage
{
    [ExcludeFromCodeCoverage]
    public class EntityFrameworkRepository<TEntity> : ITransactionalRepository<TEntity> where TEntity : class
    {
        protected readonly DbSet<TEntity> DbSet;
        protected readonly DbContext Db;
        private bool _disposed;

        public EntityFrameworkRepository(DbContext context)
        {
            Db = context;
            DbSet = Db.Set<TEntity>();
        }

        public Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> specification, int pageSize, int pageNumber)
        {
            return Task.FromResult(GetPaged(specification, pageSize, pageNumber));
        }

        public SeekResult<TEntity> Seek(ISpecification<TEntity> specification, int limit, string currentIndexOffset, SortDirection sortDirection)
        {
            throw new NotImplementedException();
        }

        public Task<SeekResult<TEntity>> SeekAsync(ISpecification<TEntity> specification, int limit, string currentIndexOffset, SortDirection sortDirection)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return Task.FromResult(GetAll());
        }

        public virtual void Add(TEntity obj)
        {
            DbSet.Add(obj);
        }

        public Task AddToCache(TEntity obj)
        {
            throw new NotImplementedException();
        }

        public Task AddAynsc(TEntity obj)
        {
            return Task.FromResult(DbSet.Add(obj));
        }

        public TEntity Get(string id)
        {
            return DbSet.Find(id);
        }

        public Task<TEntity> GetAsync(string id)
        {
            return Task.FromResult(DbSet.Find(id));
        }

        public IEnumerable<TEntity> GetAll()
        {
            return DbSet;
        }

        public IEnumerable<TEntity> GetFiltered(ISpecification<TEntity> specification)
        {
            return DbSet.Where(specification.SatisfiedBy()).ToList();
        }

        public Task<IEnumerable<TEntity>> GetFilteredAsync(ISpecification<TEntity> specification)
        {
            return Task.FromResult(GetFiltered(specification));
        }

        public PagedResult<TEntity> GetPaged(ISpecification<TEntity> specification, int pageSize, int pageNumber)
        {
            return new PagedResult<TEntity>()
            {
                CurrentPage = pageNumber,
                Items = DbSet.Where(specification.SatisfiedBy()).Skip(pageNumber -1).Take(pageNumber),
                PageSize = pageSize,
                TotalItems = Count(specification),
            };
        }

        public virtual long Count(ISpecification<TEntity> specification)
        {
            return DbSet.Count(specification.SatisfiedBy());
        }

        public Task<TEntity> GetSnapshotAsync(SnapshotQuery<TEntity> query)
        {
            throw new NotImplementedException();
        } 

        public virtual void Update(TEntity obj)
        {
            DbSet.Update(obj);
        }

        public Task UpdateAsync(TEntity obj)
        {
            Update(obj);
            return Task.FromResult(0);
        }

        public virtual void Remove(string id)
        {
            DbSet.Remove(DbSet.Find(id));
        }

        public Task RemoveAsync(string id)
        {
            Remove(id);
            return Task.FromResult(0);
        }

        public void SaveChanges()
        {
            Db.SaveChanges();
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
                Db?.Dispose();
            }

            _disposed = true;
        }

        public IEnumerable<TEntity> GetFilteredSortByDescending(ISpecification<TEntity> specification, Expression<Func<TEntity, object>> sort, int? limit)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TEntity>> GetFilteredSortByDescendingAsync(ISpecification<TEntity> specification, Expression<Func<TEntity, object>> sort, int? limit)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TEntity> GetFilteredSortBy(ISpecification<TEntity> specification, Expression<Func<TEntity, object>> sort, int? limit)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TEntity>> GetFilteredSortByAsync(ISpecification<TEntity> specification, Expression<Func<TEntity, object>> sort, int? limit)
        {
            throw new NotImplementedException();
        }
    }
}
