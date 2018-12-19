using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Models;

namespace OrdersApi.Domain.Events.Charge.Reversal
{
    public class AcquirerAccountReversalProcessed : Event
    {
        private static int _currrentTypeVersion = 1;

        public string ReversalKey { get; set; }

        public IntegrationResult Result { get; set; }

        public AcquirerAccountReversalProcessed(string aggregateKey, string correlationKey, string applicationKey,
            string sagaProcessKey, string reversalKey, short targetVersion, IntegrationResult result)
            : base(aggregateKey, correlationKey, applicationKey, targetVersion, _currrentTypeVersion, sagaProcessKey)
        {
            Result = result;
            ReversalKey = reversalKey;
        }

    }
}
