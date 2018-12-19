using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http; 
using System.Threading.Tasks;

namespace OrdersApi.Authentication
{
    public interface IIdentityService
    {
        Task<AuthenticationTicket> GetAuthenticationTicket(AuthenticationScheme Scheme, HttpRequest request);
    }
}
