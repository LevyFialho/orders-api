using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.Domain.IntegrationServices
{
    [ExcludeFromCodeCoverage]
    public class IntegrationSettings
    {
        public const string SectionName = "IntegrationSettings";

        public int ProcessingRetryInterval { get; set; }

        public int ProcessingRetryLimit { get; set; }

        public int SettlementVerificationInterval { get; set; }

        public int SettlementVerificationLimit { get; set; }

    }
}
