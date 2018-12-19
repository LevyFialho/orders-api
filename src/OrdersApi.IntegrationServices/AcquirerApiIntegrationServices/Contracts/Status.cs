using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.IntegrationServices.AcquirerApiIntegrationServices.Contracts
{
    [ExcludeFromCodeCoverage]
    public class Status
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
