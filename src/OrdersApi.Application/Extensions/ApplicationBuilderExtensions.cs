using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using OrdersApi.Application.Filters;
using OrdersApi.Application.Middleware;
using OrdersApi.Infrastructure.Settings;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage;
using Hangfire;
using Hangfire.Redis;
using Hangfire.Mongo;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.IntegrationServices.AcquirerApiIntegrationServices;
using Microsoft.Extensions.Logging;

namespace OrdersApi.Application.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder IncludeTimeElapsedHeaderInResponse(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TimeElapsedHeaderMiddleware>();
        }

        public static IApplicationBuilder UseContextTrace(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ContextTraceMiddleware>();
        }

        public static void ConfigureHangFire(this IApplicationBuilder app, IConfiguration configuration)
        {
            var settings = configuration.GetSection(HangfireSettings.SectionName).Get<HangfireSettings>();
            if (settings.UseHangfire)
            {
                if (settings.StorageType == HangfireStorageType.Redis)
                {
                    var options = new RedisStorageOptions
                    {
                        Prefix = settings.Prefix,
                        InvisibilityTimeout = TimeSpan.FromHours(settings.Timeout),
                        ExpiryCheckInterval = TimeSpan.FromSeconds(settings.ExpiryCheckInterval),
                        FetchTimeout = TimeSpan.FromSeconds(settings.FetchTimeout),
                        UseTransactions = settings.UseTransactions,
                    };
                    GlobalConfiguration.Configuration.UseRedisStorage(settings.RedisConnectionString, options);
                }
                if (settings.StorageType == HangfireStorageType.MongoDb)
                {
                    var mongoClient = ServiceCollectionExtensions.GetMongoClient(configuration);
                    GlobalConfiguration.Configuration.UseMongoStorage(mongoClient.Settings, settings.MongoDatabaseName, new MongoStorageOptions()
                    {
                        Prefix = settings.Prefix,
                        InvisibilityTimeout = TimeSpan.FromHours(settings.Timeout),
                        JobExpirationCheckInterval = TimeSpan.FromSeconds(settings.ExpiryCheckInterval),
                        QueuePollInterval = TimeSpan.FromSeconds(settings.FetchTimeout),
                    });
                }

                app.UseHangfireDashboard(settings.DashboardPath, new DashboardOptions
                {
                    Authorization = new[] { new HangFireAuthorizationFilter(), }
                });
                app.UseHangfireServer();

            }
           
        } 

        public static void ApplySqlServerEventStoreMigrations(this IApplicationBuilder app, IConfiguration configuration)
        {
            var settings = configuration.GetSection(SqlServerEventStoreSettings.SectionName).Get<SqlServerEventStoreSettings>();
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>()
                .CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<SqlEventStorageContext>(); 
                if (settings.ApplyMigrations && !settings.UseInMemory && context.Database.GetPendingMigrations().Any())
                {
                    context.Database.Migrate();
                }
            }
        }

        /// <summary>
        /// We use hangfire to dispatch unpublished events to the bus based on a CRON schedule
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configuration"></param>
        public static void ProcessUnpublishedEvents(this IApplicationBuilder app, IConfiguration configuration)
        {
            var settings = configuration.GetSection(SqlServerEventStoreSettings.SectionName).Get<SqlServerEventStoreSettings>();
            var hangfireSettings = configuration.GetSection(HangfireSettings.SectionName).Get<HangfireSettings>();
            if (!string.IsNullOrWhiteSpace(settings?.EventLogRepublishSchedule) && hangfireSettings?.UseHangfire == true)
            {
                using (var serviceScope = app.ApplicationServices.CreateScope())
                {
                    var logger = serviceScope.ServiceProvider.GetService<ILogger>();
                    var publisher = serviceScope.ServiceProvider.GetService<IEventLogPublisher>();
                    try
                    {
                        RecurringJob.AddOrUpdate(() => publisher.ProcessUnpublishedEvents(),
                            settings.EventLogRepublishSchedule);

                    }
                    catch (Exception e)
                    {
                       logger?.LogError(e.ToString()); 
                    }
                }
            }

        }
         
        public static void ScheduleSettlementVerification(this IApplicationBuilder app, IConfiguration configuration)
        {
            var settings = configuration.GetSection(AcquirerSettlementVerificationSettings.SectionName).Get<AcquirerSettlementVerificationSettings>();

            if (string.IsNullOrWhiteSpace(settings?.JobExecutionCronExpression))
                return;

            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var logger = serviceScope.ServiceProvider.GetService<ILogger>();
                var service = serviceScope.ServiceProvider.GetService<ISettlementVerificationService>();
                try
                {

                    RecurringJob.AddOrUpdate(() => service.Execute(), settings.JobExecutionCronExpression);
                }
                catch (Exception e)
                {
                    logger?.LogError(e.ToString()); 
                }
            }
        }
         
    }
}
