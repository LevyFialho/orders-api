using System;
using OrdersApi.Cqrs.Events;
#pragma warning disable S107

namespace OrdersApi.Domain.Events.Charge.Reversal
{
    public class ReversalCreated : Event
    {
        private static int _currrentTypeVersion = 1;

        public string ReversalKey { get; set; }

        public DateTime ReversalDueDate { get; set; }

        public decimal Amount { get; set; }
         

        public ReversalCreated(string aggregateKey, string correlationKey, string applicationKey, 
            string sagaProcessKey, string reversalKey, DateTime reversalDueDate, decimal amount, short targetVersion)
            : base(aggregateKey, correlationKey, applicationKey, targetVersion, _currrentTypeVersion, sagaProcessKey)
        {
            ReversalKey = reversalKey;
            ReversalDueDate = reversalDueDate;
            Amount = amount;
        }
         
    }
}
