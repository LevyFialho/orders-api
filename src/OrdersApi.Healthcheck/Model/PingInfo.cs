using System;

namespace OrdersApi.Healthcheck.Model
{

    public class PingInfo
    {
        public string ApplicationName { get; set; }

        public ApplicationTypeEnum ApplicationType { get; set; } 

        public DateTime Timestamp { get; set; }

        public string Version { get; set; }
    }
}
