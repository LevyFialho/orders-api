using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using OrdersApi.Contracts;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace OrdersApi.Application.Filters
{
    /// <summary>
    /// Authentication filter for requests of type <see cref="ClientBoundRequest"/>
    /// The value of 'ExternalClientApplicationId' field in the incoming request gets updated with the Authentication provider ID 
    /// that is bound in the current HttpContext.User object
    /// The value of 'HasAdmnistratorRights' field in the incoming request gets updated based on the Authentication provider roles
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class InjectAuthenticationFilter : IAsyncActionFilter
    { 
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments)
            {
                if (argument.Value is ClientBoundRequest request)
                {
                    var clientApplicationKey = context.HttpContext.User.FindFirstValue(ClaimTypes.System);
                    //Sets GIM ID in the request
                    request.SetExternalClientApplicationId(clientApplicationKey);
                    //Define if it has administrator rights
                    request.SetHasAdmnistratorRights(CheckAdministratorRights(context));
                    //Define if it has global query rights
                    request.SetHasGlobalQueryRights(CheckGlobalQueryRights(context));
                }
            }

            await next();
        }

        private bool CheckAdministratorRights(ActionExecutingContext context)
        {
            return context.HttpContext.User.HasClaim(ClaimTypes.Role, "Admin");
        }

        private bool CheckGlobalQueryRights(ActionExecutingContext context)
        {
            return context.HttpContext.User.HasClaim(ClaimTypes.Role, "GlobalQuery");
        }
    }
}
