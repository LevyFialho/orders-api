using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using OrdersApi.Domain.Events.Charge.Reversal;
using OrdersApi.Domain.Model.ChargeAggregate;

namespace OrdersApi.Domain.Model.Projections.ChargeProjections
{
    public class ReversalProjection
    {
        public string ReversalKey { get; set; }

        public DateTime ReversalDueDate { get; set; }

        public decimal Amount { get; set; }

        public ChargeStatus Status { get; set; }

        public DateTime? SettlementDate { get; set; }
         
        public DateTime? LastProcessingDate { get; set; }

        public DateTime? LastSettlementVerificationDate { get; set; }

        public static Func<ReversalProjection, bool> MustScheduleSettlementVerification()
        {
            return (r => r.LastSettlementVerificationDate == null && r.Status == ChargeStatus.Processed);
        }

        public List<ChargeStatusProjection> History { get; set; }

        public ReversalProjection()
        {
            History = new List<ChargeStatusProjection>();
        }

        public ReversalProjection(ReversalCreated r)
        {
            History = new List<ChargeStatusProjection>();
            ReversalKey = r.ReversalKey;
            ReversalDueDate = r.ReversalDueDate;
            Amount = r.Amount;
            Status = ChargeStatus.Created;
            History.Add(new ChargeStatusProjection()
            {
                Status = ChargeStatus.Created,
                Date = r.EventCommittedTimestamp,
            });
            
        }
    }
}
