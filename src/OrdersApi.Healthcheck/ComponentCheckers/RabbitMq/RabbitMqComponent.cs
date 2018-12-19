using OrdersApi.Healthcheck.Services;

namespace OrdersApi.Healthcheck.ComponentCheckers.RabbitMq
{
    public class RabbitMqComponent : BaseComponent
    {
        public string HostName { get; set; }

        public int Port { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }
    }
}
