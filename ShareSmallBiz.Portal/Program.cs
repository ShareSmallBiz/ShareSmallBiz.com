using ShareSmallBiz.Portal.Areas.Media;
using ShareSmallBiz.Portal.Infrastructure.Extensions;
using ShareSmallBiz.Portal.Infrastructure.Logging;
using ShareSmallBiz.Portal.Infrastructure.Middleware;
using System.Text.Json;

// Rest of the code remains unchanged
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
// OpenAPI/Swagger
// ========================
builder.Services.AddSwaggerDocumentation();

// ========================
// Application Services
// ======================
builder.Services.AddApplicationServices();

// ========================
// MVC, Razor Pages, SignalR
// ========================
builder.Services.AddMvcServices();

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
// MediaEntity Area Services
// ========================
builder.Services.AddMediaServices(builder.Configuration);



var app = builder.Build();
app.UseGlobalExceptionHandling();

// ========================
// Middleware Configuration
// ========================
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ShareSmallBiz API v1");
    options.RoutePrefix = "swagger";
});
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // ✅ Ensure authentication middleware is before authorization
app.UseAuthorization();
app.UseSession(); // ✅ Ensure session middleware runs AFTER authentication
app.UseCors("AllowAllOrigins");

// ✅ Debug Middleware to Check Authentication Status
app.Use(async (context, next) =>
{
    var user = context.User;
    var isAuthenticated = user.Identity?.IsAuthenticated ?? false;
    var userName = user.Identity?.Name ?? "Unknown";

    if (isAuthenticated)
    {
        app.Logger.LogInformation("🔹 Middleware: User is authenticated as {UserName}", userName);
    }
    else
    {
        app.Logger.LogInformation("⚠️ Middleware: User is NOT authenticated.");
    }

    var authCookie = context.Request.Cookies[".AspNetCore.Identity.Application"];
    if (authCookie != null)
    {
        app.Logger.LogInformation("🔹 Auth Cookie Found in Request");
    }
    else
    {
        app.Logger.LogInformation("⚠️ No Authentication Cookie Found in Request!");
    }

    await next();
});

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

app.Logger.LogWarning("🔹 Application started successfully.");

app.Run();
