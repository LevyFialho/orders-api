using OrdersApi.Healthcheck.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrdersApi.Healthcheck.Services
{
    public interface IComponentChecker
    {
        IEnumerable<Task<ApplicationComponentInfo>> Check();
    }
}
