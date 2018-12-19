using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; 
using OrdersApi.Healthcheck.Model;
using OrdersApi.Healthcheck.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrdersApi.Healthcheck.ComponentCheckers.RabbitMq
{
    public class RabbitMqComponentChecker : IComponentChecker
    {
        private readonly RabbitMqComponentCollection componentList;
        private readonly ILogger<IComponentChecker> logger;
        private readonly RabbitManagementApiClient rabbitManagementApiClient;

        public RabbitMqComponentChecker(IOptions<RabbitMqComponentCollection> componentList, ILogger<IComponentChecker> logger, RabbitManagementApiClient rabbitManagementApiClient)
        {
            this.componentList = componentList.Value;
            this.logger = logger;
            this.rabbitManagementApiClient = rabbitManagementApiClient;
        }

        public virtual IEnumerable<Task<ApplicationComponentInfo>> Check()
        {
            return this.componentList.Select(component => CheckComponent(component));
        }

        public virtual async Task<ApplicationComponentInfo> CheckComponent(RabbitMqComponent component)
        {
            var status = ApplicationStatusEnum.Unvailable;

            try
            {
                var result = await this.rabbitManagementApiClient.AlivenessTest(component.Name, component.UserName, component.Password);

                status = result.IsOk ? ApplicationStatusEnum.Ok : ApplicationStatusEnum.Unvailable;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking rabbitmq service '{component.Name}'", ex);
            }

            return new ApplicationComponentInfo()
            {
                ApplicationName = component.Name,
                ApplicationType = ApplicationTypeEnum.Queueservice,
                Status = status,
                Critical = component.Critical
            };
        }
    }
}

