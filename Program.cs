using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MonitoreoWeb.Data;
using MonitoreoWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<EmailService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ==========================================================
// EVITAR QUE EL NAVEGADOR GUARDE EN CACHÉ (bfcache)
// PÁGINAS AUTENTICADAS O PROTEGIDAS
// ==========================================================

app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] =
        "no-cache, no-store, must-revalidate";

    context.Response.Headers["Pragma"] = "no-cache";

    context.Response.Headers["Expires"] = "0";

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();