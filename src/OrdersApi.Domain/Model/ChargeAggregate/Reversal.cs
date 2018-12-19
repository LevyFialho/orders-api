using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Domain.Model.ChargeAggregate
{
    public class Reversal
    {
        public string ReversalKey { get; set; }

        public DateTime ReversalDueDate { get; set; }

        public decimal Amount { get; set; }

        public ChargeStatus Status { get; set; }
         
    }
}
