using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.IntegrationServices.AcquirerApiIntegrationServices
{
    public class AcquirerApiSettings
    {
        public const string SectionName = "AcquirerApiSettings";

        public string ApplicationUri { get; set; }

        public string PosRentKey { get; set; }

        public string PosRentChargeTypeCode { get; set; }

        public string ExternalPosRentKey { get; set; }

        public string ExternalPosRentChargeTypeCode { get; set; }

        public string DefaultChargeTypeCode { get; set; }

        public int DefaultAccountType { get; set; }

        public string DefaultCurrencyCode { get; set; }

        public string AuthenticationToken { get; set; }

        public bool UseMockApi { get; set; }
    }
}
