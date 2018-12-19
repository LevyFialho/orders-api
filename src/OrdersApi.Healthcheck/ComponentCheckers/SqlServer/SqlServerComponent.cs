using OrdersApi.Healthcheck.Services;

namespace OrdersApi.Healthcheck.ComponentCheckers.SqlServer
{
    public class SqlServerComponent : BaseComponent
    {
        public string ConnectionStringName { get; set; }
    }
}
