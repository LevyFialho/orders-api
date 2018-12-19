using OrdersApi.Domain.Model.ChargeAggregate;

namespace OrdersApi.Domain.Model.Projections.ChargeProjections
{
    public class AcquirerAccountInfo
    { 

        public static AcquirerAccountInfo GetAcquirerAccountInfo(IPaymentMethod method)
        {
            if (!(method is AcquirerAccount acc))
                return null;

            return new AcquirerAccountInfo
            {
                AcquirerKey = acc.AcquirerKey,
                MerchantKey = acc.MerchantKey, 
            }; 
        }

        public string AcquirerKey { get; set; }

        public string MerchantKey { get; set; }

        public int AccountType { get; set; } 
    }
}
