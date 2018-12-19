using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications; 

namespace OrdersApi.Contracts
{
    [ExcludeFromCodeCoverage]
    public abstract class GetPagedRequest<T> : ClientBoundRequest where T : class
    {
        [Required]
        [Range(1, 10000)]
        public int PageSize { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; }

        protected abstract ISpecification<T> Specification { get; }
         
        public virtual PagedQuery<T> PagedQuery()
        {
            return new PagedQuery<T>()
            {
                PageSize = PageSize,
                Specification = Specification,
                PageNumber = PageNumber,
            };
        }
    }
}
