using System.Diagnostics.CodeAnalysis;

namespace OrdersApi.Infrastructure.Settings
{
    [ExcludeFromCodeCoverage]
    public class MongoSettings
    {
        public const string SectionName = "MongoSettings";

        public string ConnectionString { get; set; } 
        public string DatabaseName { get; set; }
        public bool UseAzureCosmosDb { get; set; }
        public int SeekLimit { get; set; }
        public bool AllowSkip { get; set; }
    }

   
     
}
