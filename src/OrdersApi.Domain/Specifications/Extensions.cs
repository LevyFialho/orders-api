using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;

namespace OrdersApi.Domain.Specifications
{
    public static class Extensions
    {
        public static ISpecification<T> GetOffsetSpecification<T>(this ISpecification<T> specification,
            string currentIndexOffset, SortDirection sortDirection) where T : Projection
        {
            return string.IsNullOrWhiteSpace(currentIndexOffset)
                ? specification
                : (sortDirection == SortDirection.Asc
                    ? (specification as Specification<T>) && new DirectSpecification<T>(x =>
                          string.Compare(currentIndexOffset, x.AggregateKey, StringComparison.InvariantCultureIgnoreCase) < 0)
                    : (specification as Specification<T>) &&
                      new DirectSpecification<T>(x =>
                          string.Compare(x.AggregateKey, currentIndexOffset, StringComparison.InvariantCultureIgnoreCase) < 0));
        }
    }
}
