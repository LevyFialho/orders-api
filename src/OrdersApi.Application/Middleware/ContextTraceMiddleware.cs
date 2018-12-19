using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
#pragma warning disable S3220
#pragma warning disable S1121
namespace OrdersApi.Application.Middleware
{
    /// <summary>
    /// Middleware used to log all http requests and responses
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ContextTraceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostingEnvironment _env;
        private readonly ILogger<ContextTraceMiddleware> _logger;

        public ContextTraceMiddleware(RequestDelegate next, IHostingEnvironment env, ILogger<ContextTraceMiddleware> logger)
        {
            _next = next;
            _env = env;
            _logger = logger;
        }

        public virtual long CurrentTimestamp => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

        public async Task Invoke(HttpContext httpContext)
        {
            httpContext.Request.EnableRewind();

            await LogRequest(httpContext.Request);

            //Copy a pointer to the original response body stream
            var originalBodyStream = httpContext.Response.Body;

            //Create a new memory stream...
            using (var responseBody = new MemoryStream())
            {
                //...and use that for the temporary response body
                httpContext.Response.Body = responseBody;

                try
                {
                    await _next(httpContext);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception occurred");

                    throw;
                }
                finally
                {
                    //Format the response from the server                        
                    await LogResponse(httpContext.Response);

                    //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }


        }

        public object DeserializeObject(string text)
        {
            try
            {
                return JsonConvert.DeserializeObject(text);
            }
            catch
            {
                return text;
            }
        }

        public virtual async Task LogRequest(HttpRequest request)
        {
            var requestData = new Dictionary<string, object>();

            requestData.Add($"Request_ApplicationEnvironment", this._env.EnvironmentName);
            requestData.Add($"Request_{nameof(request.Method)}", request.Method);
            requestData.Add($"Request_{nameof(request.Path)}", request.Path);
            requestData.Add($"Request_{nameof(request.Host.Host)}", request.Host.Host);
            requestData.Add($"Request_{nameof(request.Host.Port)}", request.Host.Port);
            requestData.Add($"Request_CurrentTimestamp", CurrentTimestamp);

            var headersToExclude = new string[] { "authorization" };

            foreach (var header in request.Headers)
            {
                if (headersToExclude.Any(x => x.ToLower() == header.Key.ToLower())) continue;

                if (header.Value.Count < 0) continue;

                var key = $"Request_{header.Key}";

                if (requestData.ContainsKey(key)) continue;

                requestData.Add(key, string.Join(";", header.Value));
            }

            var requestBodyAsText = await FormatRequest(request);

            requestData.Add($"Request_Body", DeserializeObject(requestBodyAsText));
            _logger.Log(LogLevel.Trace, string.Join(Environment.NewLine, requestData));
        }

        public virtual async Task<string> FormatRequest(HttpRequest request)
        {
            var bodyAsText = string.Empty;

            if (request.ContentLength != null)
            {
                var body = request.Body;

                var buffer = new byte[Convert.ToInt32(request.ContentLength)];

                await request.Body.ReadAsync(buffer, 0, buffer.Length);

                bodyAsText = Encoding.UTF8.GetString(buffer);

                body.Seek(0, SeekOrigin.Begin);

                request.Body = body;
            }

            return bodyAsText;
        }

        public virtual string GetUserNameClaimValue(ClaimsPrincipal user)
        {
            var userNameClaimValue = string.Empty;

            var userNameClaim = user.FindFirst(ClaimTypes.Name);

            if (userNameClaim != null)
            {
                userNameClaimValue = userNameClaim.Value;
            }

            return userNameClaimValue;
        }

        public virtual async Task LogResponse(HttpResponse response)
        {
            var responseData = new Dictionary<string, object>();
            responseData.Add($"Response_ClientApplicaiton", GetUserNameClaimValue(response.HttpContext.User));

            foreach (var header in response.Headers)
            {
                responseData.Add($"Response_{header.Key}", string.Join(";", header.Value));
            }

            var responseBodyAsObjeect = DeserializeObject(await FormatResponse(response));

            responseData.Add($"Response_Body", responseBodyAsObjeect);
            responseData.Add($"Response_StatusCode", response.StatusCode);
            responseData.Add($"Response_CurrentTimestamp", CurrentTimestamp);
            _logger.Log(LogLevel.Trace, string.Join(Environment.NewLine, responseData));
        }

        public virtual async Task<string> FormatResponse(HttpResponse response)
        {
            var bodyAsText = string.Empty;

            try
            {
                response.Body.Seek(0, SeekOrigin.Begin);

                bodyAsText = await new StreamReader(response.Body).ReadToEndAsync();

                response.Body.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing request body");
            }

            return bodyAsText;
        }

    }
}
