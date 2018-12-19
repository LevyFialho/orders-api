using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using OrdersApi.Domain.Model.Projections.ChargeProjections;

namespace OrdersApi.Contracts.V1.Charge.Views
{
    [ExcludeFromCodeCoverage]
    public class ChargeReversalView
    {
        public ChargeReversalView()
        {
            
        }

        public ChargeReversalView(ReversalProjection projection)
        {
            ReversalKey = projection.ReversalKey;
            ReversalDueDate = projection.ReversalDueDate;
            Amount = projection.Amount;
            Status = projection.Status.ToString();
            SettlementDate = projection.SettlementDate;
        }

        public string ReversalKey { get; set; }

        public DateTime ReversalDueDate { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; }

        public DateTime? SettlementDate { get; set; } 
    }
}
