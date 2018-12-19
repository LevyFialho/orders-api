using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoMapper;
using System.Reflection;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.CommandHandlers;
using OrdersApi.Domain.EventHandlers;
using MediatR;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.Swagger;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Infrastructure.MessageBus;
using OrdersApi.Infrastructure.Settings;
using OrdersApi.Infrastructure.StorageProviders.MongoDb;
using OrdersApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Security.Claims;
using OrdersApi.Application.Auth;
using OrdersApi.Application.Extensions;
using OrdersApi.Application.Filters;
using OrdersApi.Domain.Commands.Charge;
using OrdersApi.Domain.Commands.Charge.Reversal;
using OrdersApi.Domain.Commands.ClientApplication;
using OrdersApi.Domain.Events.ClientApplication;
using OrdersApi.Domain.Commands.Product;
using OrdersApi.Domain.Events.Charge;
using OrdersApi.Domain.Events.Charge.Reversal;
using OrdersApi.Domain.Events.Product;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Domain.Model.Projections.ChargeProjections;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using OrdersApi.Infrastructure.StorageProviders.DocumentDb;
using OrdersApi.Infrastructure.StorageProviders.RavenDb;
using OrdersApi.IntegrationServices.AcquirerApiIntegrationServices; 
using Microsoft.ApplicationInsights.SnapshotCollector;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights.AspNetCore; 
using Microsoft.ApplicationInsights.AspNetCore.Logging;
using Microsoft.ApplicationInsights.Extensibility;
using OrdersApi.Authentication.Extensions;
using OrdersApi.Healthcheck.Extensions;


namespace OrdersApi.Application
{
    /// <summary>
    /// Web application startup class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private class SnapshotCollectorTelemetryProcessorFactory : ITelemetryProcessorFactory
        {
            private readonly IServiceProvider _serviceProvider;

            public SnapshotCollectorTelemetryProcessorFactory(IServiceProvider serviceProvider) =>
                _serviceProvider = serviceProvider;

            public ITelemetryProcessor Create(ITelemetryProcessor nextProcessor)
            {
                var snapshotConfigurationOptions = _serviceProvider.GetService<IOptions<SnapshotCollectorConfiguration>>();
                return new SnapshotCollectorTelemetryProcessor(nextProcessor, configuration: snapshotConfigurationOptions.Value);
            }
        }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApiVersionDescriptionProvider provider)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseContextTrace();

            app.UseSwagger();

            app.IncludeTimeElapsedHeaderInResponse();

            app.UseAuthentication();

            app.UseMvc();

            app.ApplySqlServerEventStoreMigrations(Configuration);

            var settings = Configuration.GetSection(MessageBrokerSettings.SectionName).Get<MessageBrokerSettings>();
            var appInsightsSettings = Configuration.GetSection(ApplicationInsightsSettings.SectionName).Get<ApplicationInsightsSettings>();
            if (appInsightsSettings?.IsEnabled == true && appInsightsSettings?.UseLogging == true)
            {
                loggerFactory.AddApplicationInsights(app.ApplicationServices, appInsightsSettings.LogLevel);
            }
            app.ConfigureHangFire(Configuration);

            if (settings.MessageBusType != MessageBusType.Inmemory)
            {
                ConfigureEventHandlersForBroker(app);
                ConfigureCommandHandlersForBroker(app);
            }

            app.ProcessUnpublishedEvents(Configuration);

            app.ScheduleSettlementVerification(Configuration);

            app.UseSwaggerUI(options =>
            {
                // build a swagger endpoint for each discovered API version
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/v{description.GroupName}/swagger.json", "v" + description.GroupName.ToUpperInvariant());
                }
            });
        }

        public IConfiguration Configuration { get; } 

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var solutionAssemblies = new List<Assembly>() { typeof(Startup).Assembly };

            services.Scan(scan => scan.FromAssemblies(solutionAssemblies).AddClasses().AsImplementedInterfaces().WithScopedLifetime());
            services.AddAutoMapper(solutionAssemblies);
            ConfigureSwagger(services);
            services.AddHealthChecks(Configuration);
            ConfigureStorage(services);
            ConfigureBus(services); 
            ConfigureIntegrationServices(services);
            ConfigureCommandHandlers(services);
            ConfigureEventHandlers(services);
            services.ConfigureHangFire(Configuration);

            RegisterQueryHandlers<ClientApplicationProjection>(services);
            RegisterQueryHandlers<ProductProjection>(services);
            RegisterQueryHandlers<ChargeProjection>(services);
            services.RegisterHttpClient(Configuration);
            services.AddScoped<ServiceFactory>(p => p.GetService); 
            services.AddScoped<IMediator, Mediator>();
            var appInsightsSettings = Configuration.GetSection(ApplicationInsightsSettings.SectionName).Get<ApplicationInsightsSettings>();
            if (appInsightsSettings?.IsEnabled == true)
            { 
                services.Configure<SnapshotCollectorConfiguration>(Configuration.GetSection(nameof(SnapshotCollectorConfiguration))); 
                services.AddSingleton<ITelemetryProcessorFactory>(sp => new SnapshotCollectorTelemetryProcessorFactory(sp));
                if (appInsightsSettings?.UseLogging == true && appInsightsSettings?.IncludeEventId == true)
                {
                    services.AddOptions<ApplicationInsightsLoggerOptions>().Configure(o => o.IncludeEventId = true);
                }
            } 

            services
                .AddMvc(options =>
                {
                    options.Filters.Add(typeof(HttpGlobalExceptionFilter));
                    options.Filters.Add(typeof(ValidateModelStateFilterAttribute));
                    var policy = new AuthorizationPolicyBuilder(new string[] { AuthenticationOptions.DefaultScheme })
                                        .RequireClaim(ClaimTypes.System)
                                        .Build();

                    options.Filters.Add(new AuthorizeFilter(policy));

                    options.Filters.Add(typeof(InjectAuthenticationFilter));

                    options.Filters.Add(typeof(ValidateModelStateFilterAttribute));

                })
                .AddManamegementEndpoints();

           

            
                services.AddCustomAuthenticationServices<CustomGimIdentityService>(Configuration.GetValue<string>("GIM:Address"));
            

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme =  AuthenticationOptions.DefaultScheme;
                }) 
                .AddCustomAuthentication(o =>
                {
                    o.ApplicationKey = Configuration.GetValue<String>("GIM:ApplicationKey");
                });
             

            services.ConfigureSettlementVerificationScheduler(Configuration);

            var container = new ContainerBuilder();

            container.Populate(services);

            return new AutofacServiceProvider(container.Build());
        }

        private void ConfigureBus(IServiceCollection services)
        {
            var settings = Configuration.GetSection(MessageBrokerSettings.SectionName).Get<MessageBrokerSettings>();
            if (settings.MessageBusType != MessageBusType.Inmemory)
            {
                services.RegisterMessageBroker(Configuration);
            }
            services.AddScoped<IMessageBus, MessageBus>();
            services.AddScoped<IQueryBus, QueryBus>();
        }

        private void ConfigureIntegrationServices(IServiceCollection services)
        {
            var settings = Configuration.GetSection(AcquirerApiSettings.SectionName).Get<AcquirerApiSettings>();
            services.AddSingleton(settings);
            if (settings.UseMockApi)
            {
                services.AddScoped<IAcquirerApiService, AcquirerApiMock>();
            }
            else
            {
                services.AddScoped<IAcquirerApiService, AcquirerApiHttpService>();
            }
        }

        private void ConfigureCommandHandlers(IServiceCollection services)
        {
            services.AddTransient<ClientApplicationCommandsHandler>();
            services.AddTransient<ProductCommandsHandler>();
            services.AddTransient<ChargeCommandsHandler>();
            services.AddTransient<ReversalCommandsHandler>();

            services.AddScoped<IRequestHandler<CreateClientApplication, MediatR.Unit>, ClientApplicationCommandsHandler>();
            services.AddScoped<IRequestHandler<ActivateClientApplication, MediatR.Unit>, ClientApplicationCommandsHandler>();
            services.AddScoped<IRequestHandler<RevokeClientApplicationCreation, MediatR.Unit>, ClientApplicationCommandsHandler>();
            services.AddScoped<IRequestHandler<UpdateProductAccess, MediatR.Unit>, ClientApplicationCommandsHandler>();

            services.AddScoped<IRequestHandler<CreateProduct, MediatR.Unit>, ProductCommandsHandler>();
            services.AddScoped<IRequestHandler<ActivateProduct, MediatR.Unit>, ProductCommandsHandler>();
            services.AddScoped<IRequestHandler<RevokeProductCreation, MediatR.Unit>, ProductCommandsHandler>();
            services.AddScoped<IRequestHandler<UpdateProductAcquirerConfiguration, MediatR.Unit>, ProductCommandsHandler>();

            services.AddScoped<IRequestHandler<CreateAcquirerAccountCharge, MediatR.Unit>, ChargeCommandsHandler>();
            services.AddScoped<IRequestHandler<SendChargeToAcquirer, MediatR.Unit>, ChargeCommandsHandler>();
            services.AddScoped<IRequestHandler<VerifyAcquirerSettlement, MediatR.Unit>, ChargeCommandsHandler>();
            services.AddScoped<IRequestHandler<ExpireCharge, MediatR.Unit>, ChargeCommandsHandler>();

            services.AddScoped<IRequestHandler<CreateChargeReversal, MediatR.Unit>, ReversalCommandsHandler>();
            services.AddScoped<IRequestHandler<ProcessAcquirerAccountReversal, MediatR.Unit>, ReversalCommandsHandler>();
            services.AddScoped<IRequestHandler<VerifyReversalSettlement, MediatR.Unit>, ReversalCommandsHandler>(); 
            
        }

        private void ConfigureEventHandlers(IServiceCollection services)
        {
            services.AddTransient<ClientApplicationEventsHandler>();
            services.AddTransient<ProductEventsHandler>();
            services.AddTransient<ChargeEventsHandler>();
            services.AddTransient<ReversalEventsHandler>();

            var integrationSettings = Configuration.GetSection(IntegrationSettings.SectionName).Get<IntegrationSettings>();
            services.AddSingleton(integrationSettings);

            var settings = Configuration.GetSection(MessageBrokerSettings.SectionName).Get<MessageBrokerSettings>();
            if (settings.MessageBusType == MessageBusType.Inmemory)
            {
                services.AddScoped<INotificationHandler<ClientApplicationCreated>, ClientApplicationEventsHandler>();
                services.AddScoped<INotificationHandler<ClientApplicationActivated>, ClientApplicationEventsHandler>();
                services.AddScoped<INotificationHandler<ClientApplicationCreationRevoked>, ClientApplicationEventsHandler>();
                services.AddScoped<INotificationHandler<ProductAccessUpdated>, ClientApplicationEventsHandler>();

                services.AddScoped<INotificationHandler<ProductCreated>, ProductEventsHandler>();
                services.AddScoped<INotificationHandler<ProductCreationRevoked>, ProductEventsHandler>();
                services.AddScoped<INotificationHandler<ProductAcquirerConfigurationUpdated>, ProductEventsHandler>();

                services.AddScoped<INotificationHandler<ChargeCreated>, ChargeEventsHandler>();
                services.AddScoped<INotificationHandler<ChargeProcessed>, ChargeEventsHandler>();
                services.AddScoped<INotificationHandler<ChargeCouldNotBeProcessed>, ChargeEventsHandler>();
                services.AddScoped<INotificationHandler<ChargeExpired>, ChargeEventsHandler>();
                services.AddScoped<INotificationHandler<ChargeSettled>, ChargeEventsHandler>();
                services.AddScoped<INotificationHandler<ChargeNotSettled>, ChargeEventsHandler>();

                services.AddScoped<INotificationHandler<ReversalCreated>, ReversalEventsHandler>();
                services.AddScoped<INotificationHandler<ReversalSettled>, ReversalEventsHandler>();
                services.AddScoped<INotificationHandler<ReversalNotSettled>, ReversalEventsHandler>();
                services.AddScoped<INotificationHandler<AcquirerAccountReversalError>, ReversalEventsHandler>();
                services.AddScoped<INotificationHandler<AcquirerAccountReversalProcessed>, ReversalEventsHandler>();

            }
        }

        private void ConfigureEventHandlersForBroker(IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

            eventBus.Subscribe<ClientApplicationCreated, ClientApplicationEventsHandler>();
            eventBus.Subscribe<ClientApplicationActivated, ClientApplicationEventsHandler>();
            eventBus.Subscribe<ClientApplicationCreationRevoked, ClientApplicationEventsHandler>();
            eventBus.Subscribe<ProductAccessUpdated, ClientApplicationEventsHandler>();

            eventBus.Subscribe<ProductCreated, ProductEventsHandler>();
            eventBus.Subscribe<ProductActivated, ProductEventsHandler>();
            eventBus.Subscribe<ProductCreationRevoked, ProductEventsHandler>();
            eventBus.Subscribe<ProductAcquirerConfigurationUpdated, ProductEventsHandler>();

            eventBus.Subscribe<ChargeCreated, ChargeEventsHandler>();
            eventBus.Subscribe<ChargeProcessed, ChargeEventsHandler>();
            eventBus.Subscribe<ChargeCouldNotBeProcessed, ChargeEventsHandler>();
            eventBus.Subscribe<ChargeExpired, ChargeEventsHandler>();
            eventBus.Subscribe<ChargeSettled, ChargeEventsHandler>();
            eventBus.Subscribe<ChargeNotSettled, ChargeEventsHandler>();

            eventBus.Subscribe<ReversalNotSettled, ReversalEventsHandler>();
            eventBus.Subscribe<ReversalSettled, ReversalEventsHandler>();
            eventBus.Subscribe<ReversalCreated, ReversalEventsHandler>();
            eventBus.Subscribe<AcquirerAccountReversalError, ReversalEventsHandler>();
            eventBus.Subscribe<AcquirerAccountReversalProcessed, ReversalEventsHandler>();
        }

        private void ConfigureCommandHandlersForBroker(IApplicationBuilder app)
        {
            var commandBus = app.ApplicationServices.GetRequiredService<ICommandBus>();

            commandBus.Subscribe<CreateClientApplication, ClientApplicationCommandsHandler>();
            commandBus.Subscribe<ActivateClientApplication, ClientApplicationCommandsHandler>();
            commandBus.Subscribe<RevokeClientApplicationCreation, ClientApplicationCommandsHandler>();

            commandBus.Subscribe<UpdateProductAccess, ClientApplicationCommandsHandler>();
            commandBus.Subscribe<CreateProduct, ProductCommandsHandler>();
            commandBus.Subscribe<ActivateProduct, ProductCommandsHandler>();
            commandBus.Subscribe<RevokeProductCreation, ProductCommandsHandler>();
            commandBus.Subscribe<UpdateProductAcquirerConfiguration, ProductCommandsHandler>();

            commandBus.Subscribe<CreateAcquirerAccountCharge, ChargeCommandsHandler>();
            commandBus.Subscribe<ExpireCharge, ChargeCommandsHandler>();
            commandBus.Subscribe<SendChargeToAcquirer, ChargeCommandsHandler>();
            commandBus.Subscribe<VerifyAcquirerSettlement, ChargeCommandsHandler>();

            commandBus.Subscribe<CreateChargeReversal, ReversalCommandsHandler>();
            commandBus.Subscribe<ProcessAcquirerAccountReversal, ReversalCommandsHandler>();
            commandBus.Subscribe<VerifyReversalSettlement, ReversalCommandsHandler>(); 
        }

        private void ConfigureStorage(IServiceCollection services)
        {
            ConfigureEventStorage(services);
            ConfigureReadStorage(services);
            ConfigureSnapshotProviders(services);
        }

        private void ConfigureEventStorage(IServiceCollection services)
        {
            services.AddSqlServerEventStoreSettings(this.Configuration);
            services.AddScoped<AggregateDataSource>();
        }

        private void ConfigureReadStorage(IServiceCollection services)
        { 
            var documentStoreSettings = Configuration.GetSection(DocumentStoreSettings.SectionName).Get<DocumentStoreSettings>();
            if (documentStoreSettings.Type == DocumentStoreType.MongoDb)
                ConfigureMongoDb(services);
            if (documentStoreSettings.Type == DocumentStoreType.RavenDb)
                ConfigureRavenDb(services);
            if (documentStoreSettings.Type == DocumentStoreType.DocumentDb)
                ConfigureDocumentDb(services);
        }

        private void ConfigureMongoDb(IServiceCollection services)
        {

            services.ConfigureMongoDbConnection(Configuration);
            services.AddScoped<IQueryableRepository<ClientApplicationProjection>, MongoRepository<ClientApplicationProjection>>();
            services.AddScoped<IQueryableRepository<ProductProjection>, MongoRepository<ProductProjection>>();
            services.AddScoped<IQueryableRepository<ChargeProjection>, MongoRepository<ChargeProjection>>();
        }

        private void ConfigureRavenDb(IServiceCollection services)
        {

            services.ConfigureRavenDbConnection(Configuration);
            services.AddScoped<IQueryableRepository<ClientApplicationProjection>, RavenDbRepository<ClientApplicationProjection>>();
            services.AddScoped<IQueryableRepository<ProductProjection>, RavenDbRepository<ProductProjection>>();
            services.AddScoped<IQueryableRepository<ChargeProjection>, RavenDbRepository<ChargeProjection>>();
        }

        private void ConfigureDocumentDb(IServiceCollection services)
        {

            services.ConfigureDocumentDbConnection(Configuration);
            services.AddScoped<IQueryableRepository<ClientApplicationProjection>, DocumentDbRepository<ClientApplicationProjection>>();
            services.AddScoped<IQueryableRepository<ProductProjection>, DocumentDbRepository<ProductProjection>>();
            services.AddScoped<IQueryableRepository<ChargeProjection>, DocumentDbRepository<ChargeProjection>>();
        }

        private void ConfigureSnapshotProviders(IServiceCollection services)
        {
            services.AddRedisSnapShotProviderSettings(this.Configuration);

            //services.AddInMemorySnapshotProviderSettings(this.Configuration);
        }
         
        private void ConfigureSwagger(IServiceCollection services)
        {
            services.AddScoped<IApiVersionDescriptionProvider, DefaultApiVersionDescriptionProvider>();
            services.AddApiVersioning(o => o.ReportApiVersions = true);
            services.AddSwaggerGen(c =>
            {
                // resolve the IApiVersionDescriptionProvider service
                var provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

                // add a swagger document for each discovered API version
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    c.SwaggerDoc("v" + description.GroupName, CreateInfoForApiVersion(description));
                }
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var actionApiVersionModel = apiDesc.ActionDescriptor?.GetApiVersion();
                    // would mean this action is unversioned and should be included everywhere
                    if (actionApiVersionModel == null || actionApiVersionModel.IsApiVersionNeutral)
                    {
                        return true;
                    }
                    if (actionApiVersionModel.DeclaredApiVersions.Any())
                    {
                        return actionApiVersionModel.DeclaredApiVersions.Any(v => $"v{v.ToString()}" == docName);
                    }
                    return actionApiVersionModel.ImplementedApiVersions.Any(v => $"v{v.ToString()}" == docName);
                });
                // add a custom operation filter which sets default values
                c.OperationFilter<SwaggerDefaultValues>();
            });
        }

        private void RegisterQueryHandlers<T>(IServiceCollection services) where T : Projection
        {
            services.AddScoped<IRequestHandler<Specification<T>, IEnumerable<T>>, QueryHandler<T>>();
            services.AddScoped<IRequestHandler<AndSpecification<T>, IEnumerable<T>>, QueryHandler<T>>();
            services.AddScoped<IRequestHandler<DirectSpecification<T>, IEnumerable<T>>, QueryHandler<T>>();
            services.AddScoped<IRequestHandler<OrSpecification<T>, IEnumerable<T>>, QueryHandler<T>>();
            services.AddScoped<IRequestHandler<NotSpecification<T>, IEnumerable<T>>, QueryHandler<T>>();
            services.AddScoped<IRequestHandler<TrueSpecification<T>, IEnumerable<T>>, QueryHandler<T>>();
            services.AddScoped<IRequestHandler<CompositeSpecification<T>, IEnumerable<T>>, QueryHandler<T>>();
            services.AddScoped<IRequestHandler<PagedQuery<T>, PagedResult<T>>, QueryHandler<T>>();
            services.AddScoped<IRequestHandler<SeekQuery<T>, SeekResult<T>>, QueryHandler<T>>();
            services.AddScoped<IRequestHandler<SnapshotQuery<T>, T>, QueryHandler<T>>();
        }

        private Info CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var swaggerSettings = Configuration.GetSection(SawaggerSettings.SectionName).Get<SawaggerSettings>();

            var contact = new Contact()
            {
                Email = swaggerSettings?.ContactEmail,
                Name = swaggerSettings?.ContactName,
                Url = swaggerSettings?.ContactUrl
            };
            var license = new License()
            {
                Name = swaggerSettings?.LicenseName,
                Url = swaggerSettings?.LicenseUrl,
            };
            var info = new Info()
            {
                Description = swaggerSettings?.Description,
                Contact = contact,
                License = license,
                TermsOfService = swaggerSettings?.TermsOfService,
                Title = swaggerSettings?.Title,
                Version = description.ApiVersion.ToString(),
            };
            if (description.IsDeprecated)
            {
                info.Description += swaggerSettings?.DeprecationMessage;
            }
            return info;
        }

       
    }
}
