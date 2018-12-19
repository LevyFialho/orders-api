using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Infrastructure.Resilience
{
    public interface IResilientHttpClientFactory
    {
        ResilientHttpClient CreateResilientHttpClient();
    }
}
