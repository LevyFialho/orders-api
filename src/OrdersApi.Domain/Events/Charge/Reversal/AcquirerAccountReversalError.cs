using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Cqrs.Events;

namespace OrdersApi.Domain.Events.Charge.Reversal
{
    public class AcquirerAccountReversalError : Event
    {
        private static int _currrentTypeVersion = 1;

        public string Message { get; set; }

        public string ReversalKey { get; set; }

        public int? StatusCode { get; set; }

        public AcquirerAccountReversalError(string aggregateKey, string correlationKey, string applicationKey,
            string sagaProcessKey, string reversalKey, short targetVersion, string message)
            : base(aggregateKey, correlationKey, applicationKey, targetVersion, _currrentTypeVersion, sagaProcessKey)
        {
            Message = message;
            ReversalKey = reversalKey;
        }

    }
}
