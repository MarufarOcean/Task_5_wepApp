using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Task_5_webApp.Data;
using Task_5_webApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database Context
builder.Services.AddDbContext<AppDBContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Data Protection - FIXED
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\temp\aspnet-keys\"))
    .SetApplicationName("Task_5_webApp");

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

// Custom Services
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IUserGuard, UserGuard>();
builder.Services.AddScoped<ILogoutService, LogoutService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // ← Only once
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Global guard middleware
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value?.ToLower() ?? "";
    // Allow authentication endpoints without user/blocked checks
    if (path.StartsWith("/account/login") ||
        path.StartsWith("/account/register") ||
        path.StartsWith("/account/confirm") ||
        path.StartsWith("/account/forgotpassword") ||
        path.StartsWith("/account/resetpassword"))
    {
        await next();
        return;
    }

    // Check existence + not blocked for authenticated areas
    var guard = ctx.RequestServices.GetRequiredService<IUserGuard>();
    var ok = await guard.CheckUserAllowedAsync(ctx);
    if (!ok)
    {
        ctx.Response.Redirect("/Account/Login");
        return;
    }
    await next();
});

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();