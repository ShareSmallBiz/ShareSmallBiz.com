using Scalar.AspNetCore;
using ShareSmallBiz.Portal.Areas.Media;
using ShareSmallBiz.Portal.Infrastructure.Extensions;
using ShareSmallBiz.Portal.Infrastructure.Logging;
using ShareSmallBiz.Portal.Infrastructure.Middleware;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ========================
// Configuration
// ========================
builder.Configuration.AddCustomConfiguration(builder.Environment);

// ========================
// Logging Configuration
// ========================
LoggingUtility.ConfigureLogging(builder, "ShareSmallBizPortal");

// ========================
// Caching Services
// ========================
builder.Services.AddCachingServices();

// ========================
// Session Configuration
// ========================
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ========================
// Database Contexts
// ========================
builder.Services.AddDatabaseContexts(builder.Configuration);

// ========================
// Identity Configuration
// ========================
builder.Services.AddIdentityServices(builder.Configuration);

// ========================
// HTTP Clients
// ========================
builder.Services.AddHttpClientUtilities(builder.Configuration);

// ========================
// OpenAPI Documentation
// ========================
builder.Services.AddOpenApiDocumentation();

// ========================
// Application Services
// ======================
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

// ========================
// MVC, Razor Pages, SignalR
// ========================
builder.Services.AddMvcServices();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddRateLimitingServices();

// ========================
// Json Serializer Configuration
// ========================
builder.Services.AddSingleton(new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
});
builder.Services.AddHttpContextAccessor();

// ========================
// Media Area Services
// ========================
builder.Services.AddMediaServices(builder.Configuration);

var app = builder.Build();
app.UseGlobalExceptionHandling();

// ========================
// Middleware Configuration
// ========================
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseCors("ApiCorsPolicy");

// ========================
// API Docs (non-production only)
// ========================
if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("ShareSmallBiz API"));
}

// ========================
// Route Configuration
// ========================
app.MapRazorPages();
app.MapMediaEndpoints();

app.MapControllerRoute(
    name: "areaRoute",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapFallbackToController("GetError", "Home");
app.MapControllerRoute(
    name: "catchAll",
    pattern: "{*slug}",
    defaults: new { controller = "Home", action = "GetError" }
);

app.MapSitemap();

app.Logger.LogWarning("Application started successfully.");

app.Run();
