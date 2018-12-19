using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.Infrastructure.Settings
{
    [ExcludeFromCodeCoverage]
    public class RedisSettings
    {
        public const string SectionName = "RedisSettings";
        public string SnapshotConnectionString { get; set; }
        public int SnapshotFrequency { get; set; }
        public int SnapshotMinutesToExpire { get; set; } 
    }
}
