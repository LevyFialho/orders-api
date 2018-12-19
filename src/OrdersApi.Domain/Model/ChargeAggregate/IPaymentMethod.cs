using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Domain.Model.ChargeAggregate
{
    public interface IPaymentMethod
    {
        PaymentMethod Method { get; }
    }
}
