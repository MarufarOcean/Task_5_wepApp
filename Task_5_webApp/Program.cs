using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Task_5_webApp.Data;
using Task_5_webApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDBContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

// IMPORTANT: Cookie auth (simple) to allow one-char passwords, not using Identity policies
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

builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IUserGuard, UserGuard>();
builder.Services.AddScoped<ILogoutService, LogoutService>();




var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

//Global guard for all requests except login/register/confirm
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value?.ToLower() ?? "";
    // IMPORTANT: allow authentication endpoints without user/blocked checks
    if (path.StartsWith("/account/login") || 
        path.StartsWith("/account/register") || 
        path.StartsWith("/account/confirm") ||
        path.StartsWith("/account/forgotpassword") ||
        path.StartsWith("/account/resetpassword"))
    {
        await next();
        return;
    }

    // nota bene: check existence + not blocked for authenticated areas
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
