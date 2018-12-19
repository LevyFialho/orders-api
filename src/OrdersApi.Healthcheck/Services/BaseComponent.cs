namespace OrdersApi.Healthcheck.Services
{
    public abstract class BaseComponent
    {
        public string Name { get; set; }

        public bool Critical { get; set; }
    }
}
