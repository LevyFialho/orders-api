using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.Infrastructure.Settings
{
    [ExcludeFromCodeCoverage]
    public class HangfireSettings
    {
        public const string SectionName = "HangfireSettings";
        public int Timeout { get; set; }
        public string Prefix { get; set; }
        public string DashboardPath { get; set; }
        public bool UseTransactions { get; set; }
        public int FetchTimeout { get; set; }
        public int ExpiryCheckInterval { get; set; }
        public int EnqueueJobRetryCount { get; set; }
        public HangfireStorageType StorageType { get; set; }
        public string MongoDatabaseName { get; set; }
        public string RedisConnectionString { get; set; }
        public bool UseHangfire { get; set; }
        public bool UseHangfireCommandScheduler { get; set; }
    }

    public enum HangfireStorageType
    {
        Redis = 0,
        MongoDb = 1}
}
