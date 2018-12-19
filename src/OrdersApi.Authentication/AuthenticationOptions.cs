using Microsoft.AspNetCore.Authentication; 

namespace OrdersApi.Authentication
{
    public class AuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "GIM";

        public string ApplicationKey { get; set; }

        public bool TracerEnabled { get; set; }
    }
}
