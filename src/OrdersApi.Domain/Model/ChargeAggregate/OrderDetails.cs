using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Cqrs.Models;

namespace OrdersApi.Domain.Model.ChargeAggregate
{
    public class OrderDetails : ValueObject<OrderDetails>
    { 
        public decimal Amount { get; set; }

        public DateTime ChargeDate { get; set; }

        public string ProductInternalKey { get; set; }
    }
}
