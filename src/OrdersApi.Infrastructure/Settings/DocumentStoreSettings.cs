using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Infrastructure.Settings
{
    public class DocumentStoreSettings
    {
        public const string SectionName = "DocumentStoreSettings";
        public DocumentStoreType Type { get; set; }
    }

    public enum DocumentStoreType
    {
        MongoDb = 1,
        RavenDb = 2,
        DocumentDb = 3
    }
}
