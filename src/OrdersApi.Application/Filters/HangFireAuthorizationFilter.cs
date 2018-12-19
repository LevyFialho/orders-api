using System.Diagnostics.CodeAnalysis;
using Hangfire.Dashboard;

namespace OrdersApi.Application.Filters
{
    /// <summary>
    /// Authorization filter for hangfire dashboard
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class HangFireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }

}
