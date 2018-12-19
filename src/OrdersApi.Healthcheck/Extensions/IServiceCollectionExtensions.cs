using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using OrdersApi.Healthcheck.ComponentCheckers.MongoDb;
using OrdersApi.Healthcheck.ComponentCheckers.RabbitMq;
using OrdersApi.Healthcheck.ComponentCheckers.Redis;
using OrdersApi.Healthcheck.Services;

namespace OrdersApi.Healthcheck.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddManagementServices(this IServiceCollection services)
        {
            services.AddScoped<IManagementService, DefaultManagementService>();

            return services;
        }

        public static IServiceCollection AddComponentChecker<TChecker, TComponentList>(this IServiceCollection services, Action<TComponentList> configureOptions) where TChecker : class, IComponentChecker
          where TComponentList : class
        {
            services.AddScoped<IComponentChecker, TChecker>();

            services.Configure(configureOptions);

            return services;
        }

        public static IServiceCollection AddMongoDbChecker(this IServiceCollection services, Action<MongoDbComponentCollection> configureOptions) 
        {
            services.AddComponentChecker<MongoDbComponentChecker, MongoDbComponentCollection>(configureOptions);

            return services;
        }

        public static IServiceCollection AddRedisChecker(this IServiceCollection services, Action<RedisComponentCollection> configureOptions)
        {
            services.AddComponentChecker<RedisComponentChecker, RedisComponentCollection>(configureOptions);

            return services;
        }

        public static IServiceCollection AddRabbitMqChecker(this IServiceCollection services, IEnumerable<RabbitMqComponent> rabbitMqComponents)
        {
            foreach (var rabbitMqComponent in rabbitMqComponents)
            {
                services.AddHttpClient(rabbitMqComponent.Name, httpClient =>
                {   
                    var uriString = $"http://{rabbitMqComponent.HostName}:{rabbitMqComponent.Port}";
                    httpClient.BaseAddress = new Uri(uriString);

                    var authenticationHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{rabbitMqComponent.UserName}:{rabbitMqComponent.Password}"));

                    httpClient
                        .DefaultRequestHeaders
                            .Authorization = new AuthenticationHeaderValue("Basic", authenticationHeaderValue);

                });
            }

            services.AddTransient<RabbitManagementApiClient>();

            services.AddComponentChecker<RabbitMqComponentChecker, RabbitMqComponentCollection>(configureOptions => 
            {
                configureOptions.Items = rabbitMqComponents;
            });

            return services;
        }
    }
}
