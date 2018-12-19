using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrdersApi.Healthcheck.Model;
using OrdersApi.Healthcheck.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace OrdersApi.Healthcheck.ComponentCheckers.SqlServer
{
    public class SqlServerComponentChecker : IComponentChecker
    {
        private readonly SqlServerComponentCollection componentList;
        private readonly ILogger<IComponentChecker> logger;
        private readonly IConfiguration configuration;

        public SqlServerComponentChecker(IOptions<SqlServerComponentCollection> componentList, ILogger<IComponentChecker> logger, IConfiguration configuration)
        {
            this.componentList = componentList.Value;
            this.logger = logger;
            this.configuration = configuration;
        }

        public virtual IEnumerable<Task<ApplicationComponentInfo>> Check()
        {
            return this.componentList.Select(component => CheckComponent(component));
        }

        public virtual async Task<ApplicationComponentInfo> CheckComponent(SqlServerComponent component)
        {
            try
            {
                var connectionString = configuration.GetConnectionString(component.ConnectionStringName);

                using (var sqlConnection = new SqlConnection(connectionString))
                {
                    await sqlConnection.OpenAsync();

                    var connectionIsOpen = sqlConnection.State == System.Data.ConnectionState.Open;

                    return new ApplicationComponentInfo()
                    {
                        ApplicationName = component.Name,
                        ApplicationType = ApplicationTypeEnum.SQLDatabase,
                        Status = connectionIsOpen ? ApplicationStatusEnum.Ok : ApplicationStatusEnum.Unvailable,
                        Critical = component.Critical
                    };
                }
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
    }
}

