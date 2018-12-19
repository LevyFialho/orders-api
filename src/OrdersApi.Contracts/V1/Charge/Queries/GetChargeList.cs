using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Domain.Model.Projections.ChargeProjections;
using OrdersApi.Domain.Specifications;
using Newtonsoft.Json;
#pragma warning disable S3776

namespace OrdersApi.Contracts.V1.Charge.Queries
{
    [ExcludeFromCodeCoverage]
    public class GetChargeList : GetPagedRequest<ChargeProjection>
    {
        public RangeFilter<decimal?> Amount { get; set; }

        public RangeFilter<DateTime?> CreatedDate { get; set; }

        public RangeFilter<DateTime?> ChargeDate { get; set; }

        [Range(0,10)]
        public int[] AccountType { get; set; }

        [Range(0, 10)]
        public string[] AcquirerKey { get; set; }

        [Range(0, 10)]
        public string[] MemberKey { get; set; }

        [Range(0, 10)]
        public string[] ProductExternalKey { get; set; }

        [Range(0, 10)]
        public string[] ProductInternalKey { get; set; }

        [Range(0, 10)]
        public string[] ProductName { get; set; }

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

        protected override ISpecification<ChargeProjection> Specification
        {
            get
            {
                Specification<ChargeProjection> spec =
                    new DirectSpecification<ChargeProjection>(x => true);
                spec = spec && ChargeSpecifications.ProjectionByPaymentMethod(PaymentMethod.AcquirerAccount);
                if (Amount != null)
                    spec = spec && ChargeSpecifications.ProjectionByAmount(Amount.From, Amount.To);
                if (ChargeDate != null)
                    spec = spec && ChargeSpecifications.ProjectionByChargeDate(ChargeDate.From, ChargeDate.To);
                if (CreatedDate != null)
                    spec = spec && ChargeSpecifications.ProjectionByCreationDate(CreatedDate.From, CreatedDate.To);
                if (AccountType != null && AccountType.Any())
                    spec = spec && ChargeSpecifications.ProjectionByAcquirerAccountType(AccountType);
                if (AcquirerKey != null && AcquirerKey.Any())
                    spec = spec && ChargeSpecifications.ProjectionByAcquirerAccountAcquirerKey(AcquirerKey);
                if (MemberKey != null && MemberKey.Any())
                    spec = spec && ChargeSpecifications.ProjectionByAcquirerAccountMemberKey(MemberKey);
                if (ProductExternalKey != null && ProductExternalKey.Any())
                    spec = spec && ChargeSpecifications.ProjectionByProductExternalKey(ProductExternalKey);
                if (ProductInternalKey != null && ProductInternalKey.Any())
                    spec = spec && ChargeSpecifications.ProjectionByProductInternalKey(ProductInternalKey);
                if (ProductName != null && ProductName.Any())
                    spec = spec && ChargeSpecifications.ProjectionByProductName(ProductName); 
                if(!string.IsNullOrWhiteSpace(InternalApplicationKey))  
                    spec = spec && ChargeSpecifications.ProjectionByApplicationKey(InternalApplicationKey);
                if (ProductInternalKeyAuthorizedList != null && ProductInternalKeyAuthorizedList.Any())
                    spec = spec && ChargeSpecifications.ProjectionByProductsAuthorized(ProductInternalKeyAuthorizedList);
          
                return spec;
            }
        }

    }
}
