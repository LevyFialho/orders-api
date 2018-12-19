using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Model.ChargeAggregate;

namespace OrdersApi.Domain.Model.Projections.ChargeProjections
{
    public class ChargeStatusProjection : ValueObject<ChargeStatusProjection>
    {
        public DateTime Date { get; set; }

        public int? IntegrationStatusCode { get; set; }

        public ChargeStatus Status { get; set; }

        public string Message { get; set; }
    }
}
