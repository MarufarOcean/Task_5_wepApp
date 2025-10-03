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
        public async Task<IActionResult> Index(string sort = "lastlogin_desc", string search = "", int page = 1, int pageSize = 10)
        {
            var q = _db.Users.AsNoTracking();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(u =>
                    u.FirstName.Contains(search) ||
                    u.LastName.Contains(search) ||
                    u.Email.Contains(search));
            }

            // Sorting logic
            q = sort switch
            {
                "name_asc" => q.OrderBy(u => u.LastName),
                "name_desc" => q.OrderByDescending(u => u.LastName),
                "email_asc" => q.OrderBy(u => u.Email),
                "email_desc" => q.OrderByDescending(u => u.Email),
                "lastlogin_asc" => q.OrderBy(u => u.LastLoginTime ?? DateTime.MinValue),
                "lastlogin_desc" => q.OrderByDescending(u => u.LastLoginTime ?? DateTime.MinValue),
                _ => q.OrderByDescending(u => u.LastLoginTime ?? DateTime.MinValue)
            };

            // Pagination logic
            var totalCount = await q.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Ensure page is within valid range
            page = Math.Max(1, Math.Min(page, totalPages));

            var users = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Sort = sort;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = totalPages;

            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);
            if (user == null) return NotFound();

            user.IsEmailVerified = true;
            user.Status = "Active";   // active after verification
            user.VerificationToken = "" ; // clear token
            await _db.SaveChangesAsync();

            TempData["Success"] = "Your email has been verified. Account is now active.";
            return RedirectToAction("Login");
        }



        [HttpPost]
        public async Task<IActionResult> Block([FromForm] int[] ids, [FromForm] int pageSize = 10, int page = 1)
        {
            var set = await _db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            foreach (var u in set) u.Status = "Blocked";
            await _db.SaveChangesAsync();
            // If current user blocked then sign out
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (ids.Contains(currentUserId))
            {
                await _logoutService.SignOutAsync(HttpContext);
                return RedirectToAction("Login", "Account");
            }

            TempData["Success"] = "Blocked successfully.";
            return RedirectToAction("Index", new { pageSize, page });

        }

        [HttpPost]
        public async Task<IActionResult> Unblock([FromForm] int[] ids, [FromForm] int pageSize = 10, int page = 1)
        {
            var set = await _db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            foreach (var u in set)
            {
                if (u.Status == "Blocked")
                {
                    u.Status = u.IsEmailVerified ? "Active" : "Unverified";
                }
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = "Unblocked successfully.";
            return RedirectToAction("Index", new { pageSize, page });
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromForm] int[] ids, [FromForm] int pageSize = 10, int page = 1)
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
            return RedirectToAction("Index", new { pageSize, page });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUnverified([FromForm] int pageSize = 10, int page = 1)
        {
            var set = await _db.Users.Where(u => u.Status == "Unverified").ToListAsync();
            _db.Users.RemoveRange(set);
            await _db.SaveChangesAsync();
            TempData["Success"] = "All unverified users deleted.";
            return RedirectToAction("Index", new { pageSize, page });
        }

    }
}
