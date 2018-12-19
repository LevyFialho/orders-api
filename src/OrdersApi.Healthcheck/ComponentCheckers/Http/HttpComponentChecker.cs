using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrdersApi.Healthcheck.Model;
using OrdersApi.Healthcheck.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http; 
using System.Threading.Tasks;

namespace OrdersApi.Healthcheck.ComponentCheckers.Http
{
    public class HttpComponentChecker : IComponentChecker
    {
        private readonly HttpComponentCollection components;
        private readonly ILogger<IComponentChecker> logger;
        private readonly IConfiguration _configuration;

        public HttpComponentChecker(IOptions<HttpComponentCollection> components, ILogger<IComponentChecker> logger, IConfiguration configuration)
        {
            this.components = components.Value;
            this.logger = logger;
            this._configuration = configuration;
        }

        public virtual IEnumerable<Task<ApplicationComponentInfo>> Check()
        {
            return this.components.Select(CheckComponent);
        }

        public void AddRequestHeaders(HttpRequestMessage requestMessage, HttpComponent component)
        {
            if (component.Headers == null || !component.Headers.Any())
                return;

            foreach (var header in component.Headers)
            {
                if (!string.IsNullOrEmpty(header.Header) && !string.IsNullOrWhiteSpace(header.Value))
                {
                    requestMessage.Headers.Add(header.Header, new List<string>() { header.Value });
                }
            }
        }

        public virtual async Task<ApplicationComponentInfo> CheckComponent(HttpComponent component)
        {
            try
            {
                using (var client = new HttpClient())
                { 

                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, component.Address);

                    AddRequestHeaders(requestMessage, component);

                    var responseMessage = await client.SendAsync(requestMessage);

                    var serviceOk = responseMessage.StatusCode == System.Net.HttpStatusCode.OK;

                    return new ApplicationComponentInfo()
                    {
                        ApplicationName = component.Name,
                        ApplicationType = ApplicationTypeEnum.Webservice,
                        Status = serviceOk ? ApplicationStatusEnum.Ok : ApplicationStatusEnum.Unvailable,
                        Critical = component.Critical
                    };
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking service '{component.Name}'", ex);

                return new ApplicationComponentInfo()
                {
                    ApplicationName = component.Name,
                    ApplicationType = ApplicationTypeEnum.Webservice,
                    Status = ApplicationStatusEnum.Unvailable,
                    Critical = component.Critical
                };
            }
        }
    }
}
