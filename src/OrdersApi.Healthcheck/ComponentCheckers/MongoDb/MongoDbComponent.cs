using OrdersApi.Healthcheck.Services;

namespace OrdersApi.Healthcheck.ComponentCheckers.MongoDb
{
    public class MongoDbComponent : BaseComponent
    {
        public string ConnectionString { get; set; }

        public string DatabaseName { get; set; }
    }
}
