using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace OrdersApi.Infrastructure.Settings
{
    public class ApplicationInsightsSettings
    {
        public const string SectionName = "ApplicationInsights";

        public string InstrumentationKey { get; set; }

        public bool IsEnabled { get; set; }

        public bool UseLogging { get; set; }

        public LogLevel LogLevel { get; set; }

        public bool IncludeEventId { get; set; }
    }
}
