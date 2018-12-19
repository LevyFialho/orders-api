using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Models; 

namespace OrdersApi.Domain.Events.Charge.Reversal
{
    public class ReversalNotSettled : Event
    {
        private static int _currrentTypeVersion = 1;
        
        public IntegrationResult Result { get; set; }

        public string ReversalKey { get; set; }

        public ReversalNotSettled(string aggregateKey, string correlationKey, string applicationKey, 
            string sagaProcessKey, short targetVersion, IntegrationResult result, string reversalKey)
            : base(aggregateKey, correlationKey, applicationKey, targetVersion, _currrentTypeVersion, sagaProcessKey)
        {
            Result = result;
            ReversalKey = reversalKey;
        }
         
    }
}
