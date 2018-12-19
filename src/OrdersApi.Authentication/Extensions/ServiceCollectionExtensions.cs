using Microsoft.Extensions.DependencyInjection;

namespace OrdersApi.Authentication.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddCustomAuthenticationServices(this IServiceCollection services, string gimAddress)
        { 
            services.AddScoped<IIdentityService, MyIdentityService>();
        }

        public static void AddCustomAuthenticationServices<TCustomIdentityService>(this IServiceCollection services, string gimAddress)
            where TCustomIdentityService : class, IIdentityService
        { 

            services.AddScoped<IIdentityService, TCustomIdentityService>();
        }
    }
}
