using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using Serilog;
using WebHotel.Data;
using WebHotel.Models;

var builder = WebApplication.CreateBuilder(args);

// Serilog — structured logging to console + rolling file
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/webhotel-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30));

// MVC + API
builder.Services.AddControllersWithViews();

// SignalR for live chat
builder.Services.AddSignalR();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "WebHotel API",
        Version = "v1",
        Description = "REST API for the WebHotel booking system. "
            + "Public endpoints: room listing and availability search. "
            + "Admin endpoints: full CRUD for rooms, bookings, customers, and payments."
    });
});

// Email service
var smtpSettings = builder.Configuration.GetSection("Smtp").Get<WebHotel.Services.SmtpSettings>()
    ?? new WebHotel.Services.SmtpSettings();
builder.Services.AddSingleton(smtpSettings);
builder.Services.AddSingleton<WebHotel.Services.IHotelEmailSender, WebHotel.Services.SmtpEmailSender>();

// Chat bot service (swap with AI provider for production)
builder.Services.AddSingleton<WebHotel.Services.IChatBotService, WebHotel.Services.FaqChatBotService>();

// Audit logging service
builder.Services.AddScoped<WebHotel.Services.IAuditService, WebHotel.Services.AuditService>();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

// Identity + Roles + EF stores
builder.Services.AddDefaultIdentity<ApplicationUser>(opts =>
{
    opts.SignIn.RequireConfirmedAccount = false;

    opts.Password.RequiredLength = 10;
    opts.Password.RequireDigit = true;
    opts.Password.RequireLowercase = true;
    opts.Password.RequireUppercase = true;
    opts.Password.RequireNonAlphanumeric = true;
    opts.Lockout.AllowedForNewUsers = true;
    opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    opts.Lockout.MaxFailedAccessAttempts = 5;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

// ✅ Cookie settings — no auto-login persistence
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    // Expire after 30 mins; not persistent
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = false;
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
});

// Identity UI (Login/Register) must be public
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToAreaFolder("Identity", "/Account");
});

// Require login everywhere by default
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.Equals("/Identity/Account/Register", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/Identity/Account/Login", StringComparison.OrdinalIgnoreCase))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                $"auth:{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(10),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }

        if (path.Equals("/CustomerRequests/Create", StringComparison.OrdinalIgnoreCase) &&
            HttpMethods.IsPost(context.Request.Method))
        {
            var requester = context.User.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                $"request:{requester}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }

        return RateLimitPartition.GetNoLimiter("unlimited");
    });
});

var app = builder.Build();

// Swagger UI (available in all environments)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "WebHotel API v1");
    options.RoutePrefix = "swagger";
});

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// ✅ Default route: go to Login if not signed in
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHub<WebHotel.Hubs.ChatHub>("/chatHub");

// ---- migrate DB, then seed roles/admin ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Roles
    foreach (var r in new[] { "Admin", "Staff", "Customer" })
        if (!await roleMgr.RoleExistsAsync(r))
            await roleMgr.CreateAsync(new IdentityRole(r));

    // Admin user (only creates, doesn’t log in)
    var adminEmail = builder.Configuration["SeedAdmin:Email"];
    var adminPassword = builder.Configuration["SeedAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var admin = await userMgr.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var createResult = await userMgr.CreateAsync(admin, adminPassword);
            if (createResult.Succeeded)
                await userMgr.AddToRoleAsync(admin, "Admin");
        }
    }
}
// -------------------------------------------

app.Run();
