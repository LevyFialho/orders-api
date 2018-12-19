using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Model.ChargeAggregate;  

namespace OrdersApi.Domain.Events.Charge
{
    public class ChargeCreated : Event
    {
        private static int _currrentTypeVersion = 1;

        public OrderDetails OrderDetails { get; set; }

        public PaymentMethodData PaymentMethodData { get; set; }

        public ChargeCreated(string aggregateKey, string correlationKey, string applicationKey, 
            string sagaProcessKey, OrderDetails orderDetails, PaymentMethodData paymentMethodData)
            : base(aggregateKey, correlationKey, applicationKey, Versions.NonExistent, _currrentTypeVersion, sagaProcessKey)
        {
            OrderDetails = orderDetails;
            PaymentMethodData = paymentMethodData;
        }
         
    }
}
