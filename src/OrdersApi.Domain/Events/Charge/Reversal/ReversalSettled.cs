using System;
using OrdersApi.Cqrs.Events; 

namespace OrdersApi.Domain.Events.Charge.Reversal
{
    public class ReversalSettled : Event
    {
        private static int _currrentTypeVersion = 1;

        public DateTime SettlementDate { get; set; } 

        public string ReversalKey { get; set; }

        public ReversalSettled(string aggregateKey, string correlationKey, string applicationKey, 
            string sagaProcessKey, short targetVersion, DateTime settlementDate, string reversalKey)
            : base(aggregateKey, correlationKey, applicationKey, targetVersion, _currrentTypeVersion, sagaProcessKey)
        {
            SettlementDate = settlementDate;
            ReversalKey = reversalKey;
        }
         
    }
}
