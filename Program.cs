//using CareerCracker.Areas.Identity.Data;
//using CareerCracker.BusinessLayer;
//using CareerCracker.Data;
//using CareerCracker.DataBaseLayer;
//using CareerCracker.Models;
//using CareerCracker.UserManagement;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.IdentityModel.Tokens;
//using System.Text;
//var builder = WebApplication.CreateBuilder(args);

//// ======================================================
//// 1?? Database
//// ======================================================
//var connectionString = builder.Configuration.GetConnectionString("AppDbContextConnection")
//    ?? throw new InvalidOperationException("Connection string not found.");

//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseNpgsql(connectionString));

//// ======================================================
//// 2?? Identity
//// ======================================================
//builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
//{
//    options.SignIn.RequireConfirmedAccount = false;
//})
//.AddEntityFrameworkStores<AppDbContext>()
//.AddDefaultTokenProviders()
//.AddDefaultUI();

//// ======================================================
//// 3?? JWT Authentication (FIXED)
//// ======================================================
//var jwt = builder.Configuration.GetSection("Jwt");
//var jwtKey = Encoding.UTF8.GetBytes(jwt["Key"]);

//builder.Services.Configure<RazorpaySettings>(
//    builder.Configuration.GetSection("Razorpay"));


//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.RequireHttpsMetadata = false;
//    options.SaveToken = true;

//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,

//        ValidIssuer = jwt["Issuer"],
//        ValidAudience = jwt["Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(jwtKey)
//    };
//});

//// ======================================================
//// 4?? Authorization
//// ======================================================
//builder.Services.AddAuthorization();

//// ======================================================
//// 5?? MVC + Razor
//// ======================================================
//builder.Services.AddControllersWithViews();
//builder.Services.AddRazorPages();

//// ======================================================
//// 6?? Dependency Injection
//// ======================================================
//builder.Services.AddScoped<IBusinessLayer, BusinessLayer>();
//builder.Services.AddScoped<IDataBaseLayer, DataBaseLayer>();
//builder.Services.AddScoped<IApplicationUserManagement, ApplicationUserManagement>();

//// ======================================================
//// 7?? CORS (MUST BE BEFORE AUTH)
//// ======================================================
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("CorsPolicy", policy =>
//    {
//        policy.AllowAnyOrigin()
//              .AllowAnyHeader()
//              .AllowAnyMethod();
//    });
//});

//var app = builder.Build();

//// ======================================================
//// 8?? DB Migrations + Master Initializer
//// ======================================================
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;

//    var db = services.GetRequiredService<AppDbContext>();
//    try { db.Database.Migrate(); }
//    catch { }

//    var master = services.GetRequiredService<IApplicationUserManagement>();
//    await master.MasterConfiguration();
//}

//// ======================================================
//// 9?? Middleware
//// ======================================================
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();

//// Must come first
//app.UseCors("CorsPolicy");

//// Authentication ? Authorization
//app.UseAuthentication();
//app.UseAuthorization();

//app.MapStaticAssets();

//// Default route
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.MapRazorPages();

//app.Run();









using CareerCracker.Areas.Identity.Data;
using CareerCracker.BusinessLayer;
using CareerCracker.Data;
using CareerCracker.DataBaseLayer;
using CareerCracker.Models;
using CareerCracker.UserManagement;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 1️⃣ Database
// ======================================================
var connectionString = builder.Configuration.GetConnectionString("AppDbContextConnection")
    ?? throw new InvalidOperationException("Connection string not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ======================================================
// 2️⃣ Identity
// ======================================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// ======================================================
// 3️⃣ JWT Authentication
// ======================================================
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = Encoding.UTF8.GetBytes(jwt["Key"]);

builder.Services.Configure<RazorpaySettings>(
    builder.Configuration.GetSection("Razorpay"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(jwtKey)
    };
});

// ======================================================
// 4️⃣ Authorization
// ======================================================
builder.Services.AddAuthorization();

// ======================================================
// 5️⃣ MVC + Razor
// ======================================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ======================================================
// 6️⃣ Dependency Injection
// ======================================================
builder.Services.AddScoped<IBusinessLayer, BusinessLayer>();
builder.Services.AddScoped<IDataBaseLayer, DataBaseLayer>();
builder.Services.AddScoped<IApplicationUserManagement, ApplicationUserManagement>();

// ======================================================
// 7️⃣ CORS (HTTPS ONLY)
// ======================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(
                "https://edtech.colaborazia.com",
                "http://localhost",
                "http://127.0.0.1",
                "http://localhost:3000",
                "http://localhost:4200"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ======================================================
// 8️⃣ FIX FOR PROXY (IMPORTANT)
// ======================================================
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ======================================================
// 9️⃣ DB Migration + Master Setup
// ======================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var db = services.GetRequiredService<AppDbContext>();
    try { db.Database.Migrate(); } catch { }

    var master = services.GetRequiredService<IApplicationUserManagement>();
    await master.MasterConfiguration();
}

// ======================================================
// 🔟 Middleware
// ======================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 🔥 FIX: detect HTTPS behind proxy
app.UseForwardedHeaders();

// 🔥 Redirect HTTP → HTTPS
app.UseHttpsRedirection();

// 🔥 BLOCK HTTP
app.Use(async (context, next) =>
{
    if (!context.Request.IsHttps)
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("HTTPS Required");
        return;
    }

    await next();
});

app.UseStaticFiles();

app.UseRouting();

// ✅ CORS
app.UseCors("CorsPolicy");

// ✅ Auth
app.UseAuthentication();
app.UseAuthorization();

// ======================================================
// 1️⃣1️⃣ Routing
// ======================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();