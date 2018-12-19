using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Infrastructure.Settings
{
    public class DocumentDbSettings
    {
        public const string SectionName = "DocumentDbSettings";

        public string DatabaseName { get; set; }

        public string Endpoint { get; set; }

        public string AuthenticationKey { get; set; } 
    }
}
