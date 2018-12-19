using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OrdersApi.Infrastructure.Resilience
{
    [ExcludeFromCodeCoverage]
    public static class HttpClientExtensions
    {
        public static async Task<T> GetJsonObjectFromHttpResponse<T>(this HttpResponseMessage response) where T : class, new()
        {
            var item = new T();
            if (response?.Content == null) return item;

            var data = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(data)) return item;

            return (T)JsonConvert.DeserializeObject(data, typeof(T));
        }

        public static async Task<string> GetStringromHttpResponse(this HttpResponseMessage response) 
        {
            if (response?.Content == null) return null;
            return await response.Content.ReadAsStringAsync(); 
        }
    }
}
