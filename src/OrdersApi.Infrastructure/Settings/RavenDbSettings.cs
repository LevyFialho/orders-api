using System;
using System.Collections.Generic;
using System.Text;
using Raven.Client.Documents.Session;
using Raven.Client.Http;

namespace OrdersApi.Infrastructure.Settings
{
    public class RavenDbSettings
    {
        public const string SectionName = "RavenDbSettings";
        public string[] Urls { get; set; } 
        public string DatabaseName { get; set; }
        public bool? UseOptimisticConcurency { get; set; }  
        public int? MaxNumberOfRequestsPerSession { get; set; }
        public bool NoTracking { get; set; }
        public bool NoCaching { get; set; } 
        public TransactionMode TransactionMode { get; set; } 
    }
}
