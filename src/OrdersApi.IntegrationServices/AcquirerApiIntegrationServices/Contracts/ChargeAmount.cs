using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.IntegrationServices.AcquirerApiIntegrationServices.Contracts
{
    [ExcludeFromCodeCoverage]
    public class ChargeAmount
    {
        public virtual decimal Amount { get; set; }

        public virtual string Currency { get; set; }
    }
}
