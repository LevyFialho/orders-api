using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.ClientApplicationAggregate; 
using OrdersApi.Domain.Model.Projections;

namespace OrdersApi.Domain.Specifications
{
    [ExcludeFromCodeCoverage]
    public static class ClientApplicationSpecifications
    {
        public static Specification<ClientApplicationProjection> ProjectionByCreationDate(DateTime? from, DateTime? to)
        {
            Specification<ClientApplicationProjection> spec = new DirectSpecification<ClientApplicationProjection>(x => true);

            if (from.HasValue)
                spec = spec && new DirectSpecification<ClientApplicationProjection>(x => x.CreatedDate >= from);
            if (to.HasValue)
                spec = spec && new DirectSpecification<ClientApplicationProjection>(x => x.CreatedDate <= to);

            return spec;
        }  

        public static Specification<ClientApplicationProjection> ProjectionByExternalKey(string key)
        {
            return new DirectSpecification<ClientApplicationProjection>(x => x.ExternalKey == key);
        }

        public static Specification<ClientApplicationProjection> ProjectionByName(string name)
        {
            return new DirectSpecification<ClientApplicationProjection>(x => x.Name == name);
        }

        public static Specification<ClientApplicationProjection> ProjectionByStatus(ClientApplicationStatus status)
        {
            return new DirectSpecification<ClientApplicationProjection>(x => x.Status == status);
        }

        public static Specification<ClientApplicationProjection> ProjectionByAggregateKey(string id)
        {
            return new DirectSpecification<ClientApplicationProjection>(x => x.AggregateKey == id);
        }

        public static Specification<ClientApplicationProjection> ProjectionByExternalKeys(string[] keys)
        {
            return new DirectSpecification<ClientApplicationProjection>(x => keys.Contains(x.ExternalKey));
        }

        public static Specification<ClientApplicationProjection> ProjectionByNames(string[] name)
        {
            return new DirectSpecification<ClientApplicationProjection>(x => name.Contains(x.Name));
        }

        public static Specification<ClientApplicationProjection> ProjectionByAggregateKeys(string[] keys)
        {
            return new DirectSpecification<ClientApplicationProjection>(x => keys.Contains(x.AggregateKey));
        }

        public static Specification<ClientApplicationProjection> VerifyAlreadyCreated(string externalKey, string aggregateKey)
        {
            return new DirectSpecification<ClientApplicationProjection>(x => x.ExternalKey == externalKey && x.AggregateKey != aggregateKey);
        }

        public static SnapshotQuery<ClientApplicationProjection> FromCacheByExternalKey(string externalKey)
        { 
            return new SnapshotQuery<ClientApplicationProjection>()
            {
                SnapshotKey = externalKey,
                Specification = ProjectionByExternalKey(externalKey) && ProjectionByStatus(ClientApplicationStatus.Active)
            };
        }

    }
}
