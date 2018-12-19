 
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Specifications;

namespace OrdersApi.Contracts.V1.ClientApplication.Queries
{
    [ExcludeFromCodeCoverage]
    public class GetClientApplication : GetRequest<ClientApplicationProjection>
    {
        public override ISpecification<ClientApplicationProjection> Specification()
        {
            return ClientApplicationSpecifications.ProjectionByAggregateKey(InternalKey);
        }
    }
}
