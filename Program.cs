using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Veritabaný baðlantýsý
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Hizmetlerin eklenmesi
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/satinAl");
});
builder.Services.AddScoped<IDbConnection>(_ => new SqlConnection(connectionString));

// Kimlik doðrulama servislerinin eklenmesi
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/login?handler=logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "KitapYurduAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.IsEssential = true;
    });

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

// Oturum ve kimlik doðrulama middleware'lerinin sýrasý önemli
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Ana sayfa için varsayýlan route
app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
    endpoints.MapGet("/", context =>
    {
        context.Response.Redirect("/MainPage");
        return Task.CompletedTask;
    });
});

app.Run();