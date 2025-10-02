using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Task_5_webApp.Services
{
    public interface ILogoutService
    {
        Task SignOutAsync(HttpContext httpContext);
    }

    public class LogoutService : ILogoutService
    {
        public async Task SignOutAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

}
