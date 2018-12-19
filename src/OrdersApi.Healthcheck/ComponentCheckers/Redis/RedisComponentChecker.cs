using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.Redis;
using OrdersApi.Healthcheck.Model;
using OrdersApi.Healthcheck.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrdersApi.Healthcheck.ComponentCheckers.Redis
{
    public class RedisComponentChecker : IComponentChecker
    {
        private readonly RedisComponentCollection componentList;
        private readonly ILogger<IComponentChecker> logger;

        public RedisComponentChecker(IOptions<RedisComponentCollection> componentList, ILogger<IComponentChecker> logger)
        {
            this.componentList = componentList.Value;
            this.logger = logger;
        }

        public virtual IEnumerable<Task<ApplicationComponentInfo>> Check()
        {
            return this.componentList.Select(component => CheckComponent(component));
        }

        public virtual Task<ApplicationComponentInfo> CheckComponent(RedisComponent component)
        {
            try
            {
                var redisClientsManager = GetClientsManager(component.ConnectionString);

                using (var redis = redisClientsManager.GetClient())
                {
                    var pingResult = redis.Ping();

                    var info = new ApplicationComponentInfo()
                    {
                        ApplicationName = component.Name,
                        ApplicationType = ApplicationTypeEnum.Other,
                        Status = pingResult ? ApplicationStatusEnum.Ok : ApplicationStatusEnum.Unvailable,
                        Critical = component.Critical
                    };

                    return Task.FromResult(info);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking database '{component.Name}'", ex);

                var info = new ApplicationComponentInfo()
                {
                    ApplicationName = component.Name,
                    ApplicationType = ApplicationTypeEnum.SQLDatabase,
                    Status = ApplicationStatusEnum.Unvailable,
                    Critical = component.Critical
                };

                return Task.FromResult(info);
            }
        }

        public virtual IRedisClientsManager GetClientsManager(string redisConnectionString)
        {
            return new RedisManagerPool(redisConnectionString);
        }
    }
}

