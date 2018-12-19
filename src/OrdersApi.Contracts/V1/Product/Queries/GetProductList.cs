using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Specifications;

namespace OrdersApi.Contracts.V1.Product.Queries
{
    [ExcludeFromCodeCoverage]
    public class GetProductList : GetPagedRequest<ProductProjection>
    {

        public string[] ExternalKeys { get; set; }

        public string[] InternalKeys { get; set; }

        protected override ISpecification<ProductProjection> Specification 
        {
            get
            {
                Specification<ProductProjection> spec =
                    new DirectSpecification<ProductProjection>(x => true);

                if (ExternalKeys != null && ExternalKeys.Any())
                    spec = spec && ProductSpecifications.ProjectionByExternalKeys(ExternalKeys);
                if (InternalKeys != null && InternalKeys.Any())
                    spec = spec && ProductSpecifications.ProjectionByAggregateKeys(InternalKeys);

                return spec;
            }
        }
         
    }
}
