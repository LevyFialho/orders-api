using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Hangfire;

namespace OrdersApi.Infrastructure.Settings
{
    [ExcludeFromCodeCoverage]
    public class SqlServerEventStoreSettings
    {
        public const string SectionName = "SqlServerEventStoreSettings";
        public string ConnectionString { get; set; } 
        public string ConnectionStringSqlite { get; set; }
        public bool UseSqlite { get; set; }
        public bool UseInMemory { get; set; }
        public bool ApplyMigrations { get; set; }
        public string EventLogRepublishSchedule { get; set; }
        public int MaxRetryCounts { get; set; }
        public int MaxRetryDelaySeconds { get; set; } 
    }
     
}
