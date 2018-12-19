namespace OrdersApi.Healthcheck.Model
{
    public class ApplicationComponentInfo : BasicApplicationInfo
    {
        public ApplicationStatusEnum Status { get; set; }

        public bool Critical { get; set; }
    }
}
