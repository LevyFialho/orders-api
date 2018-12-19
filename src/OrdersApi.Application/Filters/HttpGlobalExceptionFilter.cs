using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace OrdersApi.Application.Filters
{
    /// <summary>
    /// Filter used to set a unhandled exception response using the default <see cref="JsonErrorResponse"/> object
    /// </summary> 
    [ExcludeFromCodeCoverage]
    public partial class HttpGlobalExceptionFilter : IExceptionFilter
    {
        private readonly IHostingEnvironment _env;
        private readonly ILogger<HttpGlobalExceptionFilter> _logger;

        public HttpGlobalExceptionFilter(IHostingEnvironment env, ILogger<HttpGlobalExceptionFilter> logger)
        {
            this._env = env;
            this._logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger?.LogError(context.Exception, context.Exception.Message);

            int statusCode = StatusCodes.Status500InternalServerError;

            if (context.Exception.GetType() == typeof(UnauthorizedAccessException))
                statusCode = StatusCodes.Status401Unauthorized;
            if (context.Exception.GetType() == typeof(NotImplementedException))
                statusCode = StatusCodes.Status501NotImplemented;
            var json = new JsonErrorResponse
            {
                Errors = new JsonError[] {new JsonError()
                {
                    Code = statusCode,

                }, }

            };
            if (_env.IsDevelopment())
            {
                json.Errors[0].Message = context.Exception.ToString();
            }
            context.Result = new ObjectResult(json) { StatusCode = statusCode };
            context.HttpContext.Response.StatusCode = statusCode;
            context.ExceptionHandled = true;
        }
    }
}
