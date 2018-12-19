using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Infrastructure.Settings;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace OrdersApi.Application
{
    /// <summary>
    /// Default entry point
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Program
    {
        protected Program()
        {

        }

        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        public static void Main(string[] args)
        {
            BuildWebHost(args).Run(); //Start web app
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var appInsightsSettings = Configuration.GetSection(ApplicationInsightsSettings.SectionName).Get<ApplicationInsightsSettings>();
            if (appInsightsSettings?.IsEnabled == true)
            {
                return WebHost.CreateDefaultBuilder(args).UseApplicationInsights()
                    .UseStartup<Startup>()
                    .UseConfiguration(Configuration)
                    .Build();
            }
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseConfiguration(Configuration)
                .Build();
        }
    }
}
