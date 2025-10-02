using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Task_5_webApp.Data;
using Task_5_webApp.Models;
using Task_5_webApp.Services;

namespace Task_5_webApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDBContext _db;
        private readonly IEmailService _email;
        private readonly ILogoutService _logoutService;
        public AccountController(AppDBContext db, IEmailService email, ILogoutService logoutService)
        {
            _db = db;
            _email = email;
            _logoutService = logoutService;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string name, string desgntn, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Password must be non-empty.";
                return View();
            }

            var user = new User
            {
                Name = name,
                Designation = desgntn,
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                Status = "unverified",
                IsEmailVerified = false,
                VerificationToken = Guid.NewGuid().ToString()
            };

            try
            {
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Registered successfully. Please check your email to confirm.";
                // IMPORTANT: send email asynchronously
                //var confirmUrl = Url.Action("Confirm", "Account", new { email = user.Email }, Request.Scheme)!;
                //_ = _email.SendConfirmationAsync(user.Email, confirmUrl);

                // Build verification link
                string link = Url.Action("VerifyEmail", "Users", new { token = user.VerificationToken }, Request.Scheme);

                // Hand off to your email handler
                await _email.SendConfirmationAsync(user.Email, link);


                return RedirectToAction("Login");
            }
            catch (DbUpdateException)
            {
                // nota bene: unique index handles email duplication; we only handle error message
                TempData["Error"] = "Email is already in use.";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Confirm(string email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["Error"] = "Invalid confirmation link.";
                return RedirectToAction("Login");
            }
            if (user.Status != "blocked") user.Status = "active"; // blocked stays blocked
            await _db.SaveChangesAsync();

            TempData["Success"] = "Email confirmed. You can now log in.";
            return RedirectToAction("Login");
        }

        

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["Error"] = "Invalid credentials.";
                return View();
            }
            if (user.Status == "blocked")
            {
                TempData["Error"] = "Your account is blocked.";
                return View();
            }
            if (!PasswordHasher.Verify(password, user.PasswordHash))
            {
                TempData["Error"] = "Invalid credentials.";
                return View();
            }
            //if (!user.IsEmailVerified)
            //{
            //    TempData["Error"] = "Please verify your email before logging in.";
            //    return View();
            //}

            user.LastLoginTime = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Users");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _logoutService.SignOutAsync(HttpContext);
            return RedirectToAction("Login");
        }

    }
}
