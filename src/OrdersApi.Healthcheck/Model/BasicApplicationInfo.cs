using System; 

namespace OrdersApi.Healthcheck.Model
{

    public class BasicApplicationInfo
    {
        public string ApplicationName { get; set; }

        public ApplicationTypeEnum ApplicationType { get; set; }
        
        public DateTime BuildDate { get; set; }

        public string MachineName { get; set; }

        public ApplicationOperatingSystem OS { get; set; }

        public DateTime Timestamp { get; set; }

        public string Version { get; set; }
    }
}
