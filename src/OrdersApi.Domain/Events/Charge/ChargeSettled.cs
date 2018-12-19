using System;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Models; 

namespace OrdersApi.Domain.Events.Charge
{
    public class ChargeSettled : Event
    {
        private static int _currrentTypeVersion = 1;

        public DateTime SettlementDate { get; set; } 

        public ChargeSettled(string aggregateKey, string correlationKey, string applicationKey, 
            string sagaProcessKey, short targetVersion, DateTime settlementDate)
            : base(aggregateKey, correlationKey, applicationKey, targetVersion, _currrentTypeVersion, sagaProcessKey)
        {
            SettlementDate = settlementDate;
        }
         
    }
}
