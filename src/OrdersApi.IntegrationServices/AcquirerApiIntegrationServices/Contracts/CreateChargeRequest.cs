using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.IntegrationServices.AcquirerApiIntegrationServices.Contracts
{
    [ExcludeFromCodeCoverage]
    public class CreateChargeRequest
    {
        public string ExternalKey { get; set; }

        public DateTime ChargeDate { get; set; }

        public int AccountType { get; set; }

        public string ChargeType { get; set; }

        public ChargeAmount ChargeAmount { get; set; }
    }
}
