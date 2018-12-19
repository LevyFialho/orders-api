using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using OrdersApi.Cqrs.Queries.Specifications;

namespace OrdersApi.Cqrs.Queries
{
    public class SeekQuery<T> : IQuery<SeekResult<T>> where T : class
    {
        public ISpecification<T> Specification { get; set; } 
        public SortDirection SortDirection { get; set; }
        public int PageSize { get; set; }
        public string IndexOffset { get; set; }
    }
}
