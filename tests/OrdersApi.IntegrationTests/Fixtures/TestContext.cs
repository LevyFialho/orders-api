using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using OrdersApi.Application;
using OrdersApi.Contracts.V1.Product.Views;
using OrdersApi.IntegrationServices.LegacyService.Contracts;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json; 

namespace OrdersApi.ApplicationTests.Fixtures
{
    [ExcludeFromCodeCoverage]
    public class TestContext : IDisposable
    {
        private TestServer _server;
        public HttpClient Client { get; private set; }
        private const string AppKey = "895bf9d4-a786-4584-9cc5-dde6dfecaabe";

        public TestContext()
        {
            SetUpClient();
        }
        

        private void SetUpClient()
        {
            var appSettingsFile = Environment.GetEnvironmentVariable("AppSettingsFile");
            if (string.IsNullOrWhiteSpace(appSettingsFile))
                appSettingsFile = "appsettings.json";

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(appSettingsFile, optional: false, reloadOnChange: true)
                .Build();
            AutoMapper.ServiceCollectionExtensions.UseStaticRegistration = false;
            
            _server = new TestServer(WebHost.CreateDefaultBuilder(new string[0])
                .UseConfiguration(config)
                .UseStartup<Startup>());

            Client = _server.CreateClient();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var mockAuthentication = config.GetValue<bool>("GIM:MockAuthentication");
            if (mockAuthentication)
                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("GIM", AppKey);
        }

        public async Task<string> GetIdFromHttpResponse(HttpResponseMessage response)
        {
            if (response?.Content == null) return string.Empty;
            var data = await response.Content.ReadAsStringAsync();
            return data.Replace("\"", "");
        }

       

        public void Dispose()
        {
            _server?.Dispose();
            Client?.Dispose();
        }
    }

    public static class TestContextKeys
    {
        public static string ProductInternalKey { get; set; }
        public static string ClientApplicationInternalKey { get; set; }
        public static string AcquirerChargeInternalKey { get; set; }
        public static string AcquirerChargeReversalInternalKey { get; set; }
        public static string ProductExternalKey { get; set; }
        public static string ClientApplicationExternalKey { get; set; }
        public static string ClientApplicationProductAccessKey { get; set; }
        public const string AcquirerKey = "XPTO";
    }

}
