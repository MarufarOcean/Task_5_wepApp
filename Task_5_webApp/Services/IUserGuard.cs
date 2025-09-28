namespace Task_5_webApp.Services
{
    public interface IUserGuard
    {
        Task<bool> CheckUserAllowedAsync(HttpContext ctx);

    }
}
