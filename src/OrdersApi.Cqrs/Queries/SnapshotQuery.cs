using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries.Specifications;

namespace OrdersApi.Cqrs.Queries
{
    public class SnapshotQuery<T> : IQuery<T> where T : class
    {
        public string SnapshotKey { get; set; } 

        public ISpecification<T> Specification { get; set; }
    }
}
