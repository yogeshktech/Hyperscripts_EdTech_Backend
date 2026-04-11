using CareerCracker.Areas.Identity.Data;
using CareerCracker.BusinessLayer;
using CareerCracker.Data;
using CareerCracker.DataBaseLayer;
using CareerCracker.Models;
using CareerCracker.S3Services;
using CareerCracker.UserManagement;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
var builder = WebApplication.CreateBuilder(args);
// ======================================================
// ✅ S3 Initialization (ADD HERE)
// ======================================================
//S3StorageHelper.Initialize(builder.Configuration);
S3StorageHelper.Initialize(builder.Configuration);
// ======================================================
// 1?? Database
// ======================================================
var connectionString = builder.Configuration.GetConnectionString("AppDbContextConnection")
    ?? throw new InvalidOperationException("Connection string not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ======================================================
// 2?? Identity
// ======================================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// ======================================================
// 3?? JWT Authentication (FIXED)
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
// 4?? Authorization
// ======================================================
builder.Services.AddAuthorization();

// ======================================================
// 5?? MVC + Razor
// ======================================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ======================================================
// 6?? Dependency Injection
// ======================================================
builder.Services.AddScoped<CareerCracker.BusinessLayer.IBusinessLayer, BusinessLayer>();
builder.Services.AddScoped<IDataBaseLayer, DataBaseLayer>();
builder.Services.AddScoped<IApplicationUserManagement, ApplicationUserManagement>();

// ======================================================
// 7?? CORS (MUST BE BEFORE AUTH)
// ======================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ======================================================
// 8?? DB Migrations + Master Initializer
// ======================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var db = services.GetRequiredService<AppDbContext>();
    try { db.Database.Migrate(); }
    catch { }

    var master = services.GetRequiredService<IApplicationUserManagement>();
    await master.MasterConfiguration();
}

// ======================================================
// 9?? Middleware
// ======================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Must come first
app.UseCors("CorsPolicy");

// Authentication ? Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Attribute-routed API controllers (e.g. [Route("api/courses")], [Route("api/admin")])
app.MapControllers();

// MVC conventional route (Razor views / Home)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();









