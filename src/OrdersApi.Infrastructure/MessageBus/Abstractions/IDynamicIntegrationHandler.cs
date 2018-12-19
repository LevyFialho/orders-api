using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrdersApi.Infrastructure.MessageBus.Abstractions
{
    public interface IDynamicIntegrationHandler
    {
        Task Handle(dynamic data);
    }
}
