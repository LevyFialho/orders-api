using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Model.Projections.ChargeProjections;

namespace OrdersApi.Domain.Specifications
{
    [ExcludeFromCodeCoverage]
    public static class ChargeSpecifications
    {
        public static Specification<ChargeProjection> ProjectionByCreationDate(DateTime? from, DateTime? to)
        {
            Specification<ChargeProjection> spec = new DirectSpecification<ChargeProjection>(x => true);

            if (from.HasValue)
                spec = spec && new DirectSpecification<ChargeProjection>(x => x.CreatedDate >= from);
            if (to.HasValue)
                spec = spec && new DirectSpecification<ChargeProjection>(x => x.CreatedDate <= to);

            return spec;
        }
        

        public static Specification<ChargeProjection> ProjectionByChargeDate(DateTime? from, DateTime? to)
        {
            Specification<ChargeProjection> spec = new DirectSpecification<ChargeProjection>(x => true);

            if (from.HasValue)
                spec = spec && new DirectSpecification<ChargeProjection>(x => x.OrderDetails.ChargeDate >= from);
            if (to.HasValue)
                spec = spec && new DirectSpecification<ChargeProjection>(x => x.OrderDetails.ChargeDate <= to);

            return spec;
        }

        public static Specification<ChargeProjection> ProjectionByReversalDate(DateTime? from, DateTime? to)
        {
            Specification<ChargeProjection> spec = new DirectSpecification<ChargeProjection>(x => x.Reversals != null);

            if (from.HasValue)
                spec = spec && new DirectSpecification<ChargeProjection>(x => x.Reversals.Any(r => r.ReversalDueDate >= from));
            if (to.HasValue)
                spec = spec && new DirectSpecification<ChargeProjection>(x => x.Reversals.Any(r => r.ReversalDueDate <= to));

            return spec;
        }

        public static Specification<ChargeProjection> ProjectionByCorrelationKey(string key)
        {
            return new DirectSpecification<ChargeProjection>(x => x.CorrelationKey == key);
        }
         
        public static Specification<ChargeProjection> ProjectionByAggregateKey(string id)
        {
            return new DirectSpecification<ChargeProjection>(x => x.AggregateKey == id);
        }

        public static Specification<ChargeProjection> ProjectionByApplicationKey(string id)
        {
            return new DirectSpecification<ChargeProjection>(x => x.ApplicationKey == id);
        }

        public static Specification<ChargeProjection> ProjectionByReversalKey(string id)
        {
            return new DirectSpecification<ChargeProjection>(x => x.Reversals != null && x.Reversals.Any(r => r.ReversalKey == id));
        }

        public static Specification<ChargeProjection> ProjectionByPaymentMethod(PaymentMethod method)
        {
            return new DirectSpecification<ChargeProjection>(x => x.Method == method);
        }

        public static Specification<ChargeProjection> ProjectionByAggregateKeys(string[] keys)
        {
            return new DirectSpecification<ChargeProjection>(x => keys.Contains(x.AggregateKey));
        }

        public static Specification<ChargeProjection> ProjectionByAcquirerAccountMemberKey(string[] keys)
        {
            return new DirectSpecification<ChargeProjection>(x => x.AcquirerAccount != null && keys.Contains(x.AcquirerAccount.MerchantKey));
        }

        public static Specification<ChargeProjection> ProjectionByAcquirerAccountType(int[] keys)
        {
            return new DirectSpecification<ChargeProjection>(x => x.AcquirerAccount != null && keys.Contains(x.AcquirerAccount.AccountType));
        }
        public static Specification<ChargeProjection> ProjectionByAcquirerAccountAcquirerKey(string[] keys)
        {
            return new DirectSpecification<ChargeProjection>(x => x.AcquirerAccount != null && keys.Contains(x.AcquirerAccount.AcquirerKey));
        }

        public static Specification<ChargeProjection> ProjectionByProductInternalKey(string[] keys)
        {
            return new DirectSpecification<ChargeProjection>(x => x.Product != null && keys.Contains(x.Product.AggregateKey));
        }

        public static Specification<ChargeProjection> ProjectionByProductsAuthorized(string[] keys)
        {
            return new DirectSpecification<ChargeProjection>(x => x.Product != null && keys.Contains(x.Product.AggregateKey));
        }
         

        public static Specification<ChargeProjection> ProjectionByProductExternalKey(string[] keys)
        {
            return new DirectSpecification<ChargeProjection>(x => x.Product != null && keys.Contains(x.Product.ExternalKey));
        }

        public static Specification<ChargeProjection> ProjectionByProductName(string[] keys)
        {
            return new DirectSpecification<ChargeProjection>(x => x.Product != null && keys.Contains(x.Product.Name));
        }

        public static Specification<ChargeProjection> ProjectionByAmount(decimal? from, decimal? to)
        {
            Specification<ChargeProjection> spec = new DirectSpecification<ChargeProjection>(x => true);

            if (from.HasValue)
                spec = spec && new DirectSpecification<ChargeProjection>(x => x.OrderDetails.Amount >= from);
            if (to.HasValue)
                spec = spec && new DirectSpecification<ChargeProjection>(x => x.OrderDetails.Amount <= to);

            return spec;
        } 

        public static Specification<ChargeProjection> ProjectionByStatus(ChargeStatus status)
        {
            return new DirectSpecification<ChargeProjection>(x => x.Status == status);
        }

        public static Specification<ChargeProjection> MustScheduleSettlementVerification()
        {
            return new DirectSpecification<ChargeProjection>(ChargeProjection.MustScheduleSettlementVerification());
        }

        public static Specification<ChargeProjection> HasReversalsToVerifySettlement()
        {
            return new DirectSpecification<ChargeProjection>(ChargeProjection.MustScheduleReversalsSettlementVerification());
        }
    }
}
