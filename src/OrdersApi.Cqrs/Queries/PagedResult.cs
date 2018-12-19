using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Cqrs.Queries
{
    public class PagedResult<TEntity> where TEntity : class
    {
        public IEnumerable<TEntity> Items { get; set; }

        public long TotalItems { get; set; }

        public int CurrentPage { get; set; }

        public int PageSize { get; set; }

    }
}
