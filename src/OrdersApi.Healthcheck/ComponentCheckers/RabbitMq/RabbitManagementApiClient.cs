using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace OrdersApi.Healthcheck.ComponentCheckers.RabbitMq
{
    public class RabbitManagementApiClient
    {
        private readonly IHttpClientFactory httpClientFactory;

        public RabbitManagementApiClient(IHttpClientFactory httpClient)
        {
            this.httpClientFactory = httpClient;
        }

        public async Task<AlivenessTestResponse> AlivenessTest(string httpClientName, string username, string password)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient(httpClientName);

                var requestUrl = "api/aliveness-test/%2F";

                var httpResponseMessage = await httpClient.GetAsync(requestUrl);

                var response = await httpResponseMessage.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<AlivenessTestResponse>(response);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
