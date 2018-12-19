using Microsoft.Extensions.DependencyInjection;

namespace OrdersApi.Healthcheck.Extensions
{
    public static class MvcBuilderExtensions
    {
        public static IMvcBuilder AddManamegementEndpoints(this IMvcBuilder mvcBuilder)
        {
            return mvcBuilder.AddApplicationPart(typeof(MvcBuilderExtensions).Assembly);
        }
    }
}
