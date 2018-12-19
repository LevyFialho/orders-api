using OrdersApi.Healthcheck.Services;

namespace OrdersApi.Healthcheck.ComponentCheckers.Redis
{
    public class RedisComponent : BaseComponent
    {
        public string ConnectionString { get; set; }
    }
}
