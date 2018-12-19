using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OrdersApi.Application.Filters
{
    /// <summary>
    /// Filter used to set a validation error response using the default <see cref="JsonErrorResponse"/> object
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ValidateModelStateFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ModelState.IsValid) return;

            var errors = new List<JsonError>();

            foreach (var modelState in context.ModelState)
            {
                var key = modelState.Key;

                foreach (var error in modelState.Value.Errors)
                {
                    if (!string.IsNullOrWhiteSpace(error.ErrorMessage) || error.Exception != null)
                    {
                        var errorData = new JsonError
                        {
                            Message = $"{key}: " + (error.Exception?.Message ?? error.ErrorMessage)
                        };

                        errors.Add(errorData);
                    }
                }
            }

            context.Result = new BadRequestObjectResult(new JsonErrorResponse()
            {
                Errors = errors.ToArray()
            });
        }
    }
}
