using OrdersApi.Healthcheck.Model;
using System.Threading.Tasks;

namespace OrdersApi.Healthcheck.Services
{
    public interface IManagementService
    {
        Task<BasicApplicationInfo> GetAppInfo();

        Task<PingInfo> GetPingInfo();

        Task<ApplicationInfo> GetHealthStatus();
    }
}
