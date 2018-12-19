using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using OrdersApi.Cqrs.Commands;

namespace OrdersApi.Domain.Commands.Charge.Reversal
{
    public class ProcessAcquirerAccountReversal : Command
    {
        public string ReversalKey { get; set; }

        public ProcessAcquirerAccountReversal(string aggregateKey, string correlationKey, string applicationKey,
            string sagaProcessKey, string reversalKey)
            : base(aggregateKey, correlationKey, applicationKey, sagaProcessKey)
        {
            ReversalKey = reversalKey;
        }
         
        public override bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ReversalKey) && !string.IsNullOrWhiteSpace(AggregateKey);
        }
    }
}
