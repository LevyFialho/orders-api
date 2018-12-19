using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Hangfire;

namespace OrdersApi.Infrastructure.Settings
{
    [ExcludeFromCodeCoverage]
    public class AcquirerSettlementVerificationSettings
    {
        public const string SectionName = nameof(AcquirerSettlementVerificationSettings);

        public string JobExecutionCronExpression { get; set; }

        public int ChargesPerExecution { get; set; }
    }
     
}
