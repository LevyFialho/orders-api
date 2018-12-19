using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Models; 

namespace OrdersApi.Domain.Events.Charge
{
    public class ChargeCouldNotBeProcessed : Event
    {
        private static int _currrentTypeVersion = 1;
        
        public string Message { get; set; }

        public int? StatusCode { get; set; }

        public ChargeCouldNotBeProcessed(string aggregateKey, string correlationKey, string applicationKey, 
            string sagaProcessKey, short targetVersion, string message)
            : base(aggregateKey, correlationKey, applicationKey, targetVersion, _currrentTypeVersion, sagaProcessKey)
        {
            Message = message;
        }
         
    }
}
