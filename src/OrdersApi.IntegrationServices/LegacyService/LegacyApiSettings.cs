using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.IntegrationServices.LegacyService
{
    public class LegacyApiSettings
    {
        public const string SectionName = "LegacyApiSettings";

        public string ApplicationUri { get; set; } 

        public string DefaultChargeTypeCode { get; set; }

        public int DefaultAccountType { get; set; }

        public string DefaultCurrencyCode { get; set; }

        public string AuthenticationToken { get; set; }

        public bool UseMockApi { get; set; }
    }
}
