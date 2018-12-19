using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications; 

namespace OrdersApi.Contracts
{
    [ExcludeFromCodeCoverage]
    public abstract class SeekRequest<T> : ClientBoundRequest where T : class
    {
        [Required]
        [Range(1, 100000)]
        public int PageSize { get; set; }
         
        public string Offset { get; set; }

        public string SortDirection { get; set; }

        protected abstract ISpecification<T> Specification { get; }
         
        public virtual SeekQuery<T> PagedQuery()
        {
            return new SeekQuery<T>()
            {
                PageSize = PageSize,
                Specification = Specification,  
                IndexOffset = Offset
            };
        }
    }
}
