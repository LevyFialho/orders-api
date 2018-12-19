using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Models; 

namespace OrdersApi.Domain.Events.Charge
{
    public class ChargeExpired : Event
    {
        private static int _currrentTypeVersion = 1;

        public ChargeExpired(string aggregateKey, string correlationKey, string applicationKey, string sagaProcessKey, short targetVersion)
            : base(aggregateKey, correlationKey, applicationKey,targetVersion, _currrentTypeVersion, sagaProcessKey)
        { 
        }
         
    }
}
