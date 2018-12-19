using Microsoft.Extensions.DependencyInjection;
using OrdersApi.Healthcheck.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrdersApi.Healthcheck.Services
{
    public class DefaultManagementService : IManagementService
    {
        private readonly IEnumerable<IComponentChecker> componentCheckers;

        public DefaultManagementService(IServiceProvider provider)
        {
            this.componentCheckers = provider.GetServices<IComponentChecker>();
        }

        public Task<BasicApplicationInfo> GetAppInfo()
        {
            return Task.FromResult(ApplicationInfo.CreateBaseAppInfoDomain());
        }

        public Task<PingInfo> GetPingInfo()
        {
            return Task.FromResult(ApplicationInfo.CreatePingInfo());
        }

        public async Task<ApplicationInfo> GetHealthStatus()
        {
            var healhStatus = new ApplicationInfo(ApplicationInfo.CreateBaseAppInfoDomain());

            var components = await Task.WhenAll(componentCheckers.SelectMany(componentChecker => componentChecker.Check()));

            healhStatus.Components = components;

            var currentStatus = ApplicationStatusEnum.Ok;

            if (components.Any(c => c.Critical && c.Status != ApplicationStatusEnum.Ok))
            {
                currentStatus = ApplicationStatusEnum.Unvailable;
            }
            else if (components.Any(c => c.Status != ApplicationStatusEnum.Ok))
            {
                currentStatus = ApplicationStatusEnum.PartiallyAvaiable;
            }

            healhStatus.Status = currentStatus;

            return healhStatus;
        }
    }
}
