using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Specifications;

namespace OrdersApi.Contracts.V1.ClientApplication.Queries
{
    [ExcludeFromCodeCoverage]
    public class GetClientApplicationList : GetPagedRequest<ClientApplicationProjection>
    {

        public string[] ExternalKeys { get; set; }

        public string[] InternalKeys { get; set; }

        protected override ISpecification<ClientApplicationProjection> Specification 
        {
            get
            {
                Specification<ClientApplicationProjection> spec =
                    new DirectSpecification<ClientApplicationProjection>(x => true);

                if (ExternalKeys != null && ExternalKeys.Any())
                    spec = spec && ClientApplicationSpecifications.ProjectionByExternalKeys(ExternalKeys);
                if (InternalKeys != null && InternalKeys.Any())
                    spec = spec && ClientApplicationSpecifications.ProjectionByAggregateKeys(InternalKeys);

                return spec;
            }
        }
         
    }
}
