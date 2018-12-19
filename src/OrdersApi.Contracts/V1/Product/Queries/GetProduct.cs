using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Specifications;

namespace OrdersApi.Contracts.V1.Product.Queries
{
    [ExcludeFromCodeCoverage]
    public class GetProduct : GetRequest<ProductProjection>
    { 
        public override ISpecification<ProductProjection> Specification()
        {
            return ProductSpecifications.ProjectionByAggregateKey(InternalKey);
        }
    }
}
