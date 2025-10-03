using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.Data;
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
        public async Task<IActionResult> Register(string fname, string lname, string desgntn, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Password must be non-empty.";
                return View();
            }

            var user = new User
            {
                FirstName = fname,
                LastName = lname,
                Designation = desgntn,
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                Status = "Unverified",
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
            if (user.Status != "Blocked") user.Status = "Active"; // blocked stays blocked
            await _db.SaveChangesAsync();

            TempData["Success"] = "Email confirmed. You can now log in.";
            return RedirectToAction("Login");
        }

        //Login

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["Error"] = "Invalid credentials.";
                return View();
            }
            if (user.Status == "Blocked")
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

            user.LastLoginTime = DateTime.Now;
            await _db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim(ClaimTypes.Email, user.Email)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe, // enable "Remember Me"
                ExpiresUtc = rememberMe ? DateTimeOffset.Now.AddDays(1) : DateTimeOffset.Now.AddMinutes(30)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

            return RedirectToAction("Index", "Users");
        }

        //Forgot Password

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Please enter your email address.";
                return View();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                TempData["Success"] = "If your email is registered, you will receive a password reset link.";
                return RedirectToAction("Login");
            }

            if (user.Status == "Blocked")
            {
                TempData["Error"] = "Your account is blocked. Please contact support.";
                return View();
            }

            // Generate reset token
            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(24);
            await _db.SaveChangesAsync();

            // Send reset email
            var resetLink = Url.Action("ResetPassword", "Account", new { token = user.PasswordResetToken }, Request.Scheme);
            await _email.SendPasswordResetAsync(user.Email, resetLink!);

            TempData["Success"] = "If your email is registered, you will receive a password reset link.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid reset token.";
                return RedirectToAction("Login");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid reset token.";
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                TempData["Error"] = "Password is required.";
                ViewBag.Token = token;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                ViewBag.Token = token;
                return View();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == token &&
                u.ResetTokenExpires > DateTime.UtcNow);

            if (user == null)
            {
                TempData["Error"] = "Invalid or expired reset token.";
                return RedirectToAction("Login");
            }

            if (user.Status == "Blocked")
            {
                TempData["Error"] = "Your account is blocked. Please contact support.";
                ViewBag.Token = token;
                return View();
            }

            // Update password
            user.PasswordHash = PasswordHasher.Hash(newPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpires = null;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Password reset successfully. You can now login with your new password.";
            return RedirectToAction("Login");
        }


        //Logout

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _logoutService.SignOutAsync(HttpContext);
            return RedirectToAction("Login");
        }

    }
}
