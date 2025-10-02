using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using Task_5_webApp.Data;
using Task_5_webApp.Services;

namespace Task_5_webApp.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly AppDBContext _db;
        private readonly ILogoutService _logoutService;

        public UsersController(AppDBContext db, ILogoutService logoutService) 
        { 
            _db = db;
            _logoutService = logoutService;
        }


        [HttpGet]
        public async Task<IActionResult> Index(string sort = "lastlogin_desc")
        {
            var q = _db.Users.AsNoTracking();

            // Sorting logic
            q = sort switch
            {
                "name_asc" => q.OrderBy(u => u.Name),
                "name_desc" => q.OrderByDescending(u => u.Name),
                "email_asc" => q.OrderBy(u => u.Email),
                "email_desc" => q.OrderByDescending(u => u.Email),
                "lastlogin_asc" => q.OrderBy(u => u.LastLoginTime ?? DateTime.MinValue),
                "lastlogin_desc" => q.OrderByDescending(u => u.LastLoginTime ?? DateTime.MinValue),
                _ => q.OrderByDescending(u => u.LastLoginTime ?? DateTime.MinValue)
            };


            var users = await q.ToListAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);
            if (user == null) return NotFound();

            user.IsEmailVerified = true;
            user.Status = "active";   // active after verification
            user.VerificationToken = "" ; // clear token
            await _db.SaveChangesAsync();

            TempData["Success"] = "Your email has been verified. Account is now active.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Block([FromForm] int[] ids)
        {
            var set = await _db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            foreach (var u in set) u.Status = "blocked";
            await _db.SaveChangesAsync();
            // If current user blocked then sign out
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (ids.Contains(currentUserId))
            {
                await _logoutService.SignOutAsync(HttpContext);
                return RedirectToAction("Login", "Account");
            }

            TempData["Success"] = "Selected users blocked.";
            return RedirectToAction("Index");

        }

        [HttpPost]
        public async Task<IActionResult> Unblock([FromForm] int[] ids)
        {
            var set = await _db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            foreach (var u in set)
            {
                if (u.Status == "blocked")
                {
                    u.Status = u.IsEmailVerified ? "active" : "unverified";
                }
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = "Selected users unblocked.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromForm] int[] ids)
        {
            var set = await _db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            //hard delete (not a flag)
            _db.Users.RemoveRange(set);
            await _db.SaveChangesAsync();
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (ids.Contains(currentUserId))
            {
                await _logoutService.SignOutAsync(HttpContext);
                return RedirectToAction("Login", "Account");
            }
            TempData["Success"] = "Selected users deleted.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUnverified()
        {
            var set = await _db.Users.Where(u => u.Status == "unverified").ToListAsync();
            _db.Users.RemoveRange(set);
            await _db.SaveChangesAsync();
            TempData["Success"] = "All unverified users deleted.";
            return RedirectToAction("Index");
        }

    }
}
