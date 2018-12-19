using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.IntegrationServices.AcquirerApiIntegrationServices.Contracts
{
    [ExcludeFromCodeCoverage]
    public class CreateReversalRequest
    {
        public string  ExternalKey { get; set; }

        public ChargeAmount ChargeAmount { get; set; }
    }
}
