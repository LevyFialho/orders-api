using OrdersApi.Healthcheck.Services;

namespace OrdersApi.Healthcheck.ComponentCheckers.Http
{
    public class HttpComponent : BaseComponent
    {
        public class HttpComponentHeader
        {
            public string Header { get; set; }

            public string Value { get; set; }
        }

        public HttpComponentHeader[] Headers { get; set; }

        public string Address { get; set; }
    }


}
