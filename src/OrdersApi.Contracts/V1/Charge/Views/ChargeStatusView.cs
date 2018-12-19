using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using OrdersApi.Domain.Model.Projections.ChargeProjections;

namespace OrdersApi.Contracts.V1.Charge.Views
{
    [ExcludeFromCodeCoverage]
    public class ChargeStatusView
    {
        public ChargeStatusView()
        {
            
        }

        public ChargeStatusView(ChargeStatusProjection projection)
        {
            Date = projection.Date;
            IntegrationStatusCode = projection.IntegrationStatusCode;
            Status = projection.Status.ToString();
            Message = projection.Message;
        }

        public DateTime Date { get; set; }

        public int? IntegrationStatusCode { get; set; }

        public string Status { get; set; }

        public string Message { get; set; }
    }
}
