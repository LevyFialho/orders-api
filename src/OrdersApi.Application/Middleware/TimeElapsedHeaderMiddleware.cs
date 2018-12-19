using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace OrdersApi.Application.Middleware
{
    /// <summary>
    /// Filter used to add to the response header the 'X-Elapsed-Milliseconds' header
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class TimeElapsedHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        public TimeElapsedHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var stopWatch = new Stopwatch();

            stopWatch.Start();

            context.Response.OnStarting(state =>
            {
                var httpContext = (HttpContext)state;
                httpContext.Response.Headers.Add("X-Elapsed-Milliseconds", new[] { stopWatch.ElapsedMilliseconds.ToString() });

                return Task.FromResult(0);

            }, context);

            await _next.Invoke(context);
        }
    }
}
