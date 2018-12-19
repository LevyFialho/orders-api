using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.Infrastructure.Settings
{
    [ExcludeFromCodeCoverage]
    public class HttpClientSettings
    {
        public const string SectionName = "HttpClientSettings";
 

        public bool UseResilientHttp { get; set; }

        public int HttpClientRetryCount { get; set; }

        public int HttpClientExceptionsAllowedBeforeBreaking { get; set; }
         
    }
}
