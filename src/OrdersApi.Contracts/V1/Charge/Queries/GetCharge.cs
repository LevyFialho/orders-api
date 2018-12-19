using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Domain.Model.Projections.ChargeProjections;
using OrdersApi.Domain.Specifications;
using Newtonsoft.Json;

namespace OrdersApi.Contracts.V1.Charge.Queries
{
    [ExcludeFromCodeCoverage]
    public class GetCharge : GetRequest<ChargeProjection>
    {
        [JsonIgnore]
        protected string InternalApplicationKey { get; set; }

        [JsonIgnore]
        protected string[] ProductInternalKeyAuthorizedList { get; set; }

        public void SetInternalApplicationKey(string key)
        {
            InternalApplicationKey = key;
        }

        public void SetProductInternalKeyAuthorizedList(string[] keys)
        {
            ProductInternalKeyAuthorizedList = keys;
        }

        public override ISpecification<ChargeProjection> Specification()
        {
            var spec = ChargeSpecifications.ProjectionByAggregateKey(InternalKey);
            spec = spec && ChargeSpecifications.ProjectionByPaymentMethod(PaymentMethod.AcquirerAccount);
            if (!string.IsNullOrWhiteSpace(InternalApplicationKey))
                spec = spec && ChargeSpecifications.ProjectionByApplicationKey(InternalApplicationKey);
            if (ProductInternalKeyAuthorizedList != null && ProductInternalKeyAuthorizedList.Any())
                spec = spec && ChargeSpecifications.ProjectionByProductsAuthorized(ProductInternalKeyAuthorizedList);

            return spec;

        }
    }
}
