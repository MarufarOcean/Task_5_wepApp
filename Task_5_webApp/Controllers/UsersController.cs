using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Task_5_webApp.Data;

namespace Task_5_webApp.Controllers
{
    [Authorize(AuthenticationSchemes = "cookie")]
    public class UsersController : Controller
    {
        private readonly AppDBContext _db;

        public UsersController(AppDBContext db) { _db = db; }

        //[HttpGet]
        //public IActionResult Index() => View();

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

        [HttpPost]
        public async Task<IActionResult> Block([FromForm] int[] ids)
        {
            var set = await _db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            foreach (var u in set) u.Status = "blocked";
            await _db.SaveChangesAsync();
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
                    // Restore to previous state
                    u.Status = string.IsNullOrEmpty(u.LastLoginTime?.ToString()) ? "unverified" : "active";
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
