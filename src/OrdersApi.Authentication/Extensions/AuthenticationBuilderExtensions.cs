using System;
using Microsoft.AspNetCore.Authentication;

namespace OrdersApi.Authentication.Extensions
{
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddCustomAuthentication(this AuthenticationBuilder builder, Action<AuthenticationOptions> configureOptions)
        {
            return builder.AddScheme<AuthenticationOptions, CustomAuthHandler>(AuthenticationOptions.DefaultScheme, "My Authentication", configureOptions);
        }
    }
}
