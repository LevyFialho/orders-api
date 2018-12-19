using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Domain.Model.Projections;

namespace OrdersApi.Domain.Specifications
{
    [ExcludeFromCodeCoverage]
    public static class ProductSpecifications
    {
        public static Specification<ProductProjection> ProjectionByCreationDate(DateTime? from, DateTime? to)
        {
            Specification<ProductProjection> spec = new DirectSpecification<ProductProjection>(x => true);

            if (from.HasValue)
                spec = spec && new DirectSpecification<ProductProjection>(x => x.CreatedDate >= from);
            if (to.HasValue)
                spec = spec && new DirectSpecification<ProductProjection>(x => x.CreatedDate <= to);

            return spec;
        }

        public static Specification<ProductProjection> ProjectionByStatus(ProductStatus status)
        {
            return new DirectSpecification<ProductProjection>(x =>  x.Status == status);
        }

        public static Specification<ProductProjection> ProjectionByExternalKey(string key)
        {
            return new DirectSpecification<ProductProjection>(x => x.ExternalKey == key);
        }

        public static Specification<ProductProjection> ProjectionByName(string name)
        {
            return new DirectSpecification<ProductProjection>(x => x.Name == name);
        }

        public static Specification<ProductProjection> ProjectionByAggregateKey(string id)
        {
            return new DirectSpecification<ProductProjection>(x => x.AggregateKey == id);
        }

        public static Specification<ProductProjection> ProjectionByExternalKeys(string[] keys)
        {
            return new DirectSpecification<ProductProjection>(x => keys.Contains(x.ExternalKey));
        }

        public static Specification<ProductProjection> ProjectionByNames(string[] name)
        {
            return new DirectSpecification<ProductProjection>(x => name.Contains(x.Name));
        }

        public static Specification<ProductProjection> ProjectionByAggregateKeys(string[] keys)
        {
            return new DirectSpecification<ProductProjection>(x => keys.Contains(x.AggregateKey));
        }

        public static SnapshotQuery<ProductProjection> ProjectionSnapshot(string externalKey)
        { 
            return new SnapshotQuery<ProductProjection>()
            {
                SnapshotKey = externalKey,
                Specification = ProjectionByExternalKey(externalKey) && ProjectionByStatus(ProductStatus.Active),
            };
        }

    }
}
