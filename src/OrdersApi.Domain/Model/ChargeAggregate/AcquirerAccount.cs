namespace OrdersApi.Domain.Model.ChargeAggregate
{
    public class AcquirerAccount : IPaymentMethod
    {
        public string AcquirerKey { get; set; }

        public string MerchantKey { get; set; } 
        
        public PaymentMethod Method => PaymentMethod.AcquirerAccount;
    }
}
