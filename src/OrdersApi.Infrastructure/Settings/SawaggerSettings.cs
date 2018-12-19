using System.Diagnostics.CodeAnalysis;

namespace OrdersApi.Infrastructure.Settings
{
    [ExcludeFromCodeCoverage]
    public class SawaggerSettings
    {
        public const string SectionName = "SwaggerSettings";
        public string ContactEmail { get; set; }
        public string ContactName { get; set; }
        public string ContactUrl { get; set; }
        public string LicenseName { get; set; }
        public string LicenseUrl { get; set; }
        public string Description { get; set; }
        public string TermsOfService { get; set; }
        public string Title { get; set; }
        public string DeprecationMessage { get; set; }
    }
}
