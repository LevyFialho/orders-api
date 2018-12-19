using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using Autofac;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Infrastructure.MessageBus;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using OrdersApi.Infrastructure.MessageBus.CommandBus;
using OrdersApi.Infrastructure.MessageBus.EventBus;
using OrdersApi.Infrastructure.MessageBus.RabbitMQ;
using OrdersApi.Infrastructure.MessageBus.ServiceBus;
using OrdersApi.Infrastructure.Resilience;
using OrdersApi.Infrastructure.Settings;
using OrdersApi.Infrastructure.StorageProviders.InMemory;
using OrdersApi.Infrastructure.StorageProviders.Redis;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventLog.Services;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage;
using OrdersApi.IntegrationServices.AcquirerApiIntegrationServices;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Redis;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client;
using StackExchange.Redis;
using OrdersApi.Healthcheck.ComponentCheckers.Http;
using OrdersApi.Healthcheck.ComponentCheckers.RabbitMq;
using OrdersApi.Healthcheck.ComponentCheckers.SqlServer;
using OrdersApi.Healthcheck.Extensions;
using DocumentClient = Microsoft.Azure.Documents.Client.DocumentClient;

namespace OrdersApi.Application.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureHangFire(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(HangfireSettings.SectionName).Get<HangfireSettings>();
            var brokerSettings = configuration.GetSection(MessageBrokerSettings.SectionName).Get<MessageBrokerSettings>();
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
                    services.AddHangfire(x => x.UseRedisStorage(settings.RedisConnectionString, options));
                }
                if (settings.StorageType == HangfireStorageType.MongoDb)
                {
                    var mongoClient = GetMongoClient(configuration);
                    services.AddHangfire(x => x.UseMongoStorage(mongoClient.Settings, settings.MongoDatabaseName, new MongoStorageOptions()
                    {
                        Prefix = settings.Prefix,
                        InvisibilityTimeout = TimeSpan.FromHours(settings.Timeout),
                        JobExpirationCheckInterval = TimeSpan.FromSeconds(settings.ExpiryCheckInterval),
                        QueuePollInterval = TimeSpan.FromSeconds(settings.FetchTimeout),
                    }));
                }

                if (settings.UseHangfireCommandScheduler && brokerSettings?.UseDefaultCommandScheduler != true)
                    services.AddScoped<ICommandScheduler, HangfireCommandScheduler>();

            }
        }

        public static void RegisterMessageBroker(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(MessageBrokerSettings.SectionName).Get<MessageBrokerSettings>();

            services.Configure<MessageBrokerSettings>(messageBrokerSettings => 
            {
                configuration.GetSection(MessageBrokerSettings.SectionName).Bind(messageBrokerSettings);
            });

            if (settings.MessageBusType == MessageBusType.Azzure)
            {
                services.AddSingleton<IServiceBusPersisterConnection>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<DefaultServiceBusPersisterConnection>>();
                    var eventBusConnection = new ServiceBusConnectionStringBuilder(settings.EventBusConnection);
                    var commandBusConnection = new ServiceBusConnectionStringBuilder(settings.CommandBusConnection);
                    return new DefaultServiceBusPersisterConnection(eventBusConnection, logger, settings.RetryCount,
                        settings.MinimumRetryBackoffSeconds, settings.MaximumRetryBackoffSeconds, commandBusConnection);
                });
                services.AddSingleton<IEventBus, AzureEventBus>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<AzureEventBus>>();
                    var serviceBusPersisterConnection = sp.GetRequiredService<IServiceBusPersisterConnection>();
                    var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                    var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                    return new AzureEventBus(serviceBusPersisterConnection,
                        eventBusSubcriptionsManager, settings, iLifetimeScope, logger);
                });
                services.AddSingleton<ICommandBus, AzureCommandBus>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<AzureCommandBus>>();
                    var serviceBusPersisterConnection = sp.GetRequiredService<IServiceBusPersisterConnection>();
                    var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                    var commandBusSubcriptionsManager = sp.GetRequiredService<ICommandBusSubscriptionsManager>();

                    return new AzureCommandBus(serviceBusPersisterConnection,
                        commandBusSubcriptionsManager, settings, iLifetimeScope, logger);
                });
            }

            if (settings.MessageBusType == MessageBusType.RabbitMq)
            {
                services.AddSingleton<IEventBusRabbitMQPersistentConnection>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<DefaultRabbitMqPersistentConnection>>();

                    //Event Bus
                    var factory = new ConnectionFactory()
                    {
                        HostName = settings.RabbitMqConnection
                    };

                    if (!string.IsNullOrEmpty(settings.RabbitMqUserName))
                    {
                        factory.UserName = settings.RabbitMqUserName;
                    }

                    if (!string.IsNullOrEmpty(settings.RabbitMqPassword))
                    {
                        factory.Password = settings.RabbitMqPassword;
                    }

                    return new DefaultRabbitMqPersistentConnection(factory, logger, settings.RetryCount);
                });

                services.AddSingleton<ICommandBusRabbitMQPersistentConnection>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<DefaultRabbitMqPersistentConnection>>();

                    //Command Bus
                    var commandBusFactory = new ConnectionFactory()
                    {
                        HostName = settings.RabbitMqCommandBusConnection
                    };

                    if (!string.IsNullOrEmpty(settings.RabbitMqUserName))
                    {
                        commandBusFactory.UserName = settings.RabbitMqCommandBusUserName;
                    }

                    if (!string.IsNullOrEmpty(settings.RabbitMqPassword))
                    {
                        commandBusFactory.Password = settings.RabbitMqCommandBusPassword;
                    }

                    return new DefaultRabbitMqPersistentConnection(commandBusFactory, logger, settings.RetryCount);
                });

                services.AddSingleton<IEventBus, RabbitMQEventBus>();

                services.AddSingleton<ICommandBus, RabbitMQCommandBus>();
            }

            if (settings.UseDefaultCommandScheduler)
                services.AddScoped<ICommandScheduler, CommandScheduler>();

            services.AddSingleton<ICommandBusSubscriptionsManager, InMemoryCommandBusSubscriptionsManager>();
            services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();

        }

        public static void RegisterHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(HttpClientSettings.SectionName).Get<HttpClientSettings>();
            if (settings.UseResilientHttp)
            {
                services.AddSingleton<IResilientHttpClientFactory, ResilientHttpClientFactory>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<ResilientHttpClient>>();
                    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                    return new ResilientHttpClientFactory(logger, httpContextAccessor, settings.HttpClientExceptionsAllowedBeforeBreaking, settings.HttpClientRetryCount);
                });
                services.AddSingleton<IHttpClient, ResilientHttpClient>(sp => sp.GetService<IResilientHttpClientFactory>().CreateResilientHttpClient());
            }
            else
            {
                services.AddSingleton<IHttpClient, StandardHttpClient>();
            }
        }

        public static void ConfigureMongoDbConnection(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(MongoSettings.SectionName).Get<MongoSettings>();
            var mongoClient = GetMongoClient(configuration);
            var database = mongoClient.GetDatabase(settings.DatabaseName);
            services.AddScoped<IMongoDatabase>(_ => database);
            services.AddSingleton<MongoSettings>(settings);
        }

        public static void ConfigureRavenDbConnection(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(RavenDbSettings.SectionName).Get<RavenDbSettings>();
            services.AddSingleton<RavenDbSettings>(settings);
        }

        public static void ConfigureDocumentDbConnection(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(DocumentDbSettings.SectionName).Get<DocumentDbSettings>();
            services.AddSingleton<DocumentDbSettings>(settings);
            services.AddScoped<DocumentClient>(sp => new DocumentClient(new Uri(settings.Endpoint), settings.AuthenticationKey));
        }

        public static MongoClient GetMongoClient(IConfiguration configuration)
        {
            var settings = configuration.GetSection(MongoSettings.SectionName).Get<MongoSettings>();
            MongoClient mongoClient = null;
            if (settings.UseAzureCosmosDb)
            {
                var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(settings.ConnectionString));
                clientSettings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
                mongoClient = new MongoClient(clientSettings);
            }
            else
            {
                mongoClient = new MongoClient(settings.ConnectionString);
            }
            return mongoClient;
        }

        public static void AddRedisSnapShotProviderSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RedisSettings>(options => configuration.GetSection(RedisSettings.SectionName).Bind(options));

            services.AddSingleton(serviceProvider =>
            {
                var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>();

                var configurationOptions = ConfigurationOptions.Parse(redisSettings.SnapshotConnectionString);

                return StackExchange.Redis.ConnectionMultiplexer.Connect(configurationOptions);
            });

            services.AddScoped<ISnapshotStorageProvider, RedisSnapshotStorageProvider>();
        }

        public static void AddInMemorySnapshotProviderSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RedisSettings>(options => configuration.GetSection(RedisSettings.SectionName).Bind(options));

            services.AddScoped<ISnapshotStorageProvider, InMemorySnapshotStorageProvider>();
        }

        public static void AddSqlServerEventStoreSettings(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(SqlServerEventStoreSettings.SectionName).Get<SqlServerEventStoreSettings>();
            if (settings.UseInMemory)
                services.AddDbContext<SqlEventStorageContext>(options => options.UseInMemoryDatabase("SqlEventStorage"));
            else if (settings.UseSqlite)
                services.AddDbContext<SqlEventStorageContext>(options => options.UseSqlite(settings.ConnectionStringSqlite));
            else
                services.AddDbContext<SqlEventStorageContext>(
                    options => options.UseSqlServer(settings.ConnectionString,
                        sqlServerOptionsAction: sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(settings.MaxRetryCounts, TimeSpan.FromSeconds(settings.MaxRetryDelaySeconds), null);
                        }));
            services.AddScoped<ITransactionalRepository<EventData>, EntityFrameworkEventStorage>();
            services.AddScoped<IEventStorageProvider, SqlServerEventStorageProvider>();
            services.AddScoped<IEventLogService, EventLogService>();
            services.AddScoped<IEventLogPublisher, EventLogPublisher>();
            services.AddScoped<DbContext, SqlEventStorageContext>();

        }

        public static void AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddManagementServices();

            //Add and configure checkers
            services.AddComponentChecker<SqlServerComponentChecker, SqlServerComponentCollection>(componentCollection =>
            {
                configuration.GetSection("ManagementConfiguration:SqlServerComponents").Bind(componentCollection.Items);
            });

            services.AddComponentChecker<HttpComponentChecker, HttpComponentCollection>(componentCollection =>
            {
                configuration.GetSection("ManagementConfiguration:HttpComponents").Bind(componentCollection.Items);
            });

            services.AddMongoDbChecker(componentCollection =>
            {
                configuration.GetSection("ManagementConfiguration:MongoDbComponents").Bind(componentCollection.Items);
            });

            services.AddRedisChecker(componentCollection =>
            {
                configuration.GetSection("ManagementConfiguration:RedisComponents").Bind(componentCollection.Items);
            });

            var rabbitMqComponents = configuration.GetSection("ManagementConfiguration:RabbitMqComponents").Get<IEnumerable<RabbitMqComponent>>();

            if (rabbitMqComponents != null)
                services.AddRabbitMqChecker(rabbitMqComponents);
        }

        public static void ConfigureSettlementVerificationScheduler(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AcquirerSettlementVerificationSettings>(settings => 
            {
                configuration.GetSection(AcquirerSettlementVerificationSettings.SectionName).Bind(settings);
            });

            services.AddTransient<ISettlementVerificationService, AcquirerSettlementVerificationService>();
        }
    }
}
