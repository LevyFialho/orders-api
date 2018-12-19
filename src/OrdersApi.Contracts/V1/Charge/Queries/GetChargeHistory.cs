using System;
using System.ComponentModel.DataAnnotations;
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
    public class GetChargeHistory : ClientBoundRequest
    {
        [JsonIgnore]
        protected string InternalApplicationKey { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int PageSize { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; }

        [JsonIgnore]
        protected string ChargeOrderKey { get; set; }

        [JsonIgnore]
        protected string[] ProductInternalKeyAuthorizedList { get; set; }

        public virtual void SetInternalApplicationKey(string key)
        {
            InternalApplicationKey = key;
        }

        public virtual void SetInternalChargeOrderKey(string key)
        {
            ChargeOrderKey = key;
        }

        public virtual void SetProductInternalKeyAuthorizedList(string[] keys)
        {
            ProductInternalKeyAuthorizedList = keys;
        }

        public virtual ISpecification<ChargeProjection> Specification()
        {
             
                var spec = ChargeSpecifications.ProjectionByAggregateKey(ChargeOrderKey); 
                if (!string.IsNullOrWhiteSpace(InternalApplicationKey))
                    spec = spec && ChargeSpecifications.ProjectionByApplicationKey(InternalApplicationKey);
                if (ProductInternalKeyAuthorizedList != null && ProductInternalKeyAuthorizedList.Any())
                    spec = spec && ChargeSpecifications.ProjectionByProductsAuthorized(
                               ProductInternalKeyAuthorizedList);

                return spec;
            
        }
    }
}
