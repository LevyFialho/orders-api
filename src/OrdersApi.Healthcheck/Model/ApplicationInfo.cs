using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace OrdersApi.Healthcheck.Model
{
    public class ApplicationInfo : BasicApplicationInfo
    {
        public ApplicationInfo()
        {

        }

        public ApplicationInfo(BasicApplicationInfo basicApplicationInfo)
        {
            this.ApplicationName = basicApplicationInfo.ApplicationName;
            this.ApplicationType = basicApplicationInfo.ApplicationType;
            this.BuildDate = basicApplicationInfo.BuildDate;
            this.MachineName = basicApplicationInfo.MachineName;
            this.OS = basicApplicationInfo.OS;
            this.Timestamp = basicApplicationInfo.Timestamp;
            this.Version = basicApplicationInfo.Version;
        }

        public ICollection<ApplicationComponentInfo> Components { get; set; } = new List<ApplicationComponentInfo>();

        public ApplicationStatusEnum Status { get; set; }

        public static BasicApplicationInfo CreateBaseAppInfoDomain()
        {
            return new BasicApplicationInfo()
            {
                ApplicationName = Assembly.GetEntryAssembly().GetName().Name,
                ApplicationType = ApplicationTypeEnum.Webservice,
                Version = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                BuildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).CreationTime,
                MachineName = Environment.MachineName,
                Timestamp = DateTime.Now,
                OS = new ApplicationOperatingSystem()
                {
                    Name = Environment.OSVersion.Platform.ToString(),
                    Version = Environment.OSVersion.Version.ToString()
                }
            };
        }

        public static PingInfo CreatePingInfo()
        {
            return new PingInfo()
            {
                ApplicationName = Assembly.GetEntryAssembly().GetName().Name,
                ApplicationType = ApplicationTypeEnum.Webservice,
                Version = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                Timestamp = DateTime.UtcNow,
            };
        }
    }
}
