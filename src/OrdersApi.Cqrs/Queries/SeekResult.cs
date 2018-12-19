using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Cqrs.Queries
{
    public class SeekResult<TEntity> where TEntity : class
    {
        public IEnumerable<TEntity> Items { get; set; } 

        public string CurrentIndexOffset { get; set; }

        public string NextIndexOffset { get; set; }

        public int PageSize { get; set; }

    }
}
