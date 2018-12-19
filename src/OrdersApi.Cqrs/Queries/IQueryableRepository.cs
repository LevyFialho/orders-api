using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Queries.Specifications;

namespace OrdersApi.Cqrs.Queries
{
    public interface IQueryableRepository<TEntity>: IDisposable where TEntity : class
    {
        TEntity Get(string id);

        Task<TEntity> GetAsync(string id);

        IEnumerable<TEntity> GetAll();

        IEnumerable<TEntity> GetFiltered(ISpecification<TEntity> specification);

        Task<IEnumerable<TEntity>> GetFilteredAsync(ISpecification<TEntity> specification);

        IEnumerable<TEntity> GetFilteredSortByDescending(ISpecification<TEntity> specification, Expression<Func<TEntity, object>> sort, int? limit);

        Task<IEnumerable<TEntity>> GetFilteredSortByDescendingAsync(ISpecification<TEntity> specification, Expression<Func<TEntity, object>> sort, int? limit);

        IEnumerable<TEntity> GetFilteredSortBy(ISpecification<TEntity> specification, Expression<Func<TEntity, object>> sort, int? limit);

        Task<IEnumerable<TEntity>> GetFilteredSortByAsync(ISpecification<TEntity> specification, Expression<Func<TEntity, object>> sort, int? limit);
                
        PagedResult<TEntity> GetPaged(ISpecification<TEntity> specification, int pageSize, int pageNumber);

        Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> specification, int pageSize, int pageNumber);

        SeekResult<TEntity> Seek(ISpecification<TEntity> specification, int limit, string currentIndexOffset, SortDirection sortDirection);

        Task<SeekResult<TEntity>> SeekAsync(ISpecification<TEntity> specification, int limit, string currentIndexOffset, SortDirection sortDirection);

        Task<IEnumerable<TEntity>> GetAllAsync();

        void Add(TEntity obj);
        
        Task AddAynsc(TEntity obj);

        void Update(TEntity obj);

        Task UpdateAsync(TEntity obj);

        void Remove(string id);

        Task RemoveAsync(string id);

        long Count(ISpecification<TEntity> specification);
         
    }

    public interface ITransactionalRepository<TEntity> : IQueryableRepository<TEntity> where TEntity : class
    {
        void SaveChanges();
    }
}
