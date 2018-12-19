using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage
{
    [ExcludeFromCodeCoverage]
    public class Payload
    {
        public string EventType { get; set; }

        public string AggregateType { get; set; }

        public string EventData { get; set; }
    }
}
