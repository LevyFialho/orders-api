using OrdersApi.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace OrdersApi.Authentication
{
    public class CustomAuthHandler : AuthenticationHandler<AuthenticationOptions>
    {
        private readonly ILogger<CustomAuthHandler> _logger;
        private readonly IIdentityService _identityService;

        public CustomAuthHandler(IOptionsMonitor<AuthenticationOptions> options,
                                 ILoggerFactory loggerFactory,
                                 UrlEncoder encoder,
                                 ISystemClock clock,
                                 IIdentityService identityService) : base(options, loggerFactory, encoder, clock)
        {
            _logger = loggerFactory.CreateLogger<CustomAuthHandler>();

            _identityService = identityService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                if (Options?.TracerEnabled == true)
                    _logger.LogError("Authorization header not found");

                return AuthenticateResult.NoResult();
            }

            if (!AuthenticationHeaderValue.TryParse(Request.Headers["Authorization"], out AuthenticationHeaderValue authenticationHeaderValue))
            {
                if (Options?.TracerEnabled == true)
                    _logger.LogError("Error parsing Authorization header");

                return AuthenticateResult.NoResult();
            }

            if (!AuthenticationOptions.DefaultScheme.Equals(authenticationHeaderValue.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                if (Options?.TracerEnabled == true)
                    _logger.LogError($"Schema '{AuthenticationOptions.DefaultScheme}' not found.");

                return AuthenticateResult.NoResult();
            }

            try
            {
                var ticket = await _identityService.GetAuthenticationTicket(Scheme, Request);
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting authentication ticket", ex);
                return AuthenticateResult.Fail(ex);
            }
        }
    }
}
