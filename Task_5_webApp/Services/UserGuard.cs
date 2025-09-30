using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Task_5_webApp.Data;

namespace Task_5_webApp.Services
{
    public class UserGuard : IUserGuard
    {
        private readonly AppDBContext _db;
        public UserGuard(AppDBContext db) { _db = db; }

        public async Task<bool> CheckUserAllowedAsync(HttpContext ctx)
        {
            //returns false if not authenticated, not found, or blocked
            var idStr = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idStr)) return false;

            if (!int.TryParse(idStr, out var userId)) return false;

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            if (user.Status == "blocked") return false;

            return true;
        }

    }
}
