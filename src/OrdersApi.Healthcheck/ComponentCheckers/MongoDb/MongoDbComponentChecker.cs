using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OrdersApi.Healthcheck.Model;
using OrdersApi.Healthcheck.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrdersApi.Healthcheck.ComponentCheckers.MongoDb
{
    public class MongoDbComponentChecker : IComponentChecker
    {
        private readonly MongoDbComponentCollection componentList;
        private readonly ILogger<IComponentChecker> logger;

        public MongoDbComponentChecker(IOptions<MongoDbComponentCollection> componentList, ILogger<IComponentChecker> logger)
        {
            this.componentList = componentList.Value;
            this.logger = logger;
        }

        public virtual IEnumerable<Task<ApplicationComponentInfo>> Check()
        {
            return this.componentList.Select(component => CheckComponent(component));
        }

        public virtual async Task<ApplicationComponentInfo> CheckComponent(MongoDbComponent component)
        {
            try
            {
                var mongoClient = GetMongoClient(component.ConnectionString);
                
                var database = mongoClient.GetDatabase(component.DatabaseName);

                var command = new PingCommand(); 

                var result = await database.RunCommandAsync(command);
                
                return new ApplicationComponentInfo()
                {
                    ApplicationName = component.Name,
                    ApplicationType = ApplicationTypeEnum.Other,
                    Status = result.Ok ? ApplicationStatusEnum.Ok : ApplicationStatusEnum.Unvailable,
                    Critical = component.Critical
                };
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking database '{component.Name}'", ex);

                return new ApplicationComponentInfo()
                {
                    ApplicationName = component.Name,
                    ApplicationType = ApplicationTypeEnum.SQLDatabase,
                    Status = ApplicationStatusEnum.Unvailable,
                    Critical = component.Critical
                };
            }
        }

        public virtual IMongoClient GetMongoClient(string connectionString)
        {
            return new MongoClient(connectionString);
        }
    }
}

