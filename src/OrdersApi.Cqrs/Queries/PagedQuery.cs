using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Cqrs.Queries.Specifications;

namespace OrdersApi.Cqrs.Queries
{
    public class PagedQuery<T> : IQuery<PagedResult<T>> where T : class
    {
        public ISpecification<T> Specification { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
