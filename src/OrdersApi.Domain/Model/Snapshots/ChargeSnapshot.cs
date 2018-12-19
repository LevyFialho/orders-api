using System.Collections.Generic;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.Model.ChargeAggregate; 

namespace OrdersApi.Domain.Model.Snapshots
{
    public class ChargeSnapshot : Snapshot
    {
        public OrderDetails OrderDetails { get; set; }

        public PaymentMethodData PaymentMethodData { get; set; }

        public ChargeStatus Status { get; set; }

        public List<Reversal> Reversals { get; set; }

        public ChargeSnapshot(string snapshotKey, Charge charge) : base(snapshotKey, charge.AggregateKey, charge.CurrentVersion)
        {
            Status = charge.Status;
            OrderDetails = charge.OrderDetails;
            PaymentMethodData = charge.PaymentMethodData;
            Reversals = charge.Reversals;
        }
         
    }
}
