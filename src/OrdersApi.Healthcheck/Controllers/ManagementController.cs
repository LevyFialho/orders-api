using Microsoft.AspNetCore.Mvc;
using OrdersApi.Healthcheck.Model;
using OrdersApi.Healthcheck.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace OrdersApi.Healthcheck.Controllers
{
    [Route("[controller]")]
    [ApiVersionNeutral]
    [Authorize]
    public class ManagementController : Controller
    {
        private readonly IManagementService _managementService;

        public ManagementController(IManagementService managementService)
        {
            this._managementService = managementService;
        }

        /// <summary>
        /// Ping  your application
        /// </summary>
        /// <returns>An action result with the body of your applicaction info</returns>
        [HttpGet("ping")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PingInfo), 200)]
        public async Task<IActionResult> Ping()
        {
            var info = await _managementService.GetPingInfo();
            return Ok(info);
        }


        /// <summary>
        /// Get the health-check info of your application
        /// </summary>
        /// <returns>An action result with the body of your applicaction info</returns>
        [HttpGet("app-info")]
        [ProducesResponseType(typeof(BasicApplicationInfo), 200)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAppInfo()
        {
            var appInfo = await _managementService.GetAppInfo();

            return Ok(appInfo);
        }

        /// <summary>
        /// Get the health-check info of your application and their dependencies
        /// </summary>
        /// <returns>An action result with the body of your applicaction info and dependencies</returns>
        [HttpGet("health-check")]
        [ProducesResponseType(typeof(ApplicationInfo), 200)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetHealthCheck()
        {
            var healthStatus = await _managementService.GetHealthStatus();

            return Ok(healthStatus);
        }
    }
}
