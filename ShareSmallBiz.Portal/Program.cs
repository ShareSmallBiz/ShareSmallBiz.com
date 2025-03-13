using HttpClientUtility.MemoryCache;
using HttpClientUtility.RequestResult;
using HttpClientUtility.StringConverter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Extensions;
using ShareSmallBiz.Portal.Infrastructure.Logging;
using ShareSmallBiz.Portal.Infrastructure.Middleware;
using ShareSmallBiz.Portal.Infrastructure.Services;
using ShareSmallBiz.Portal.Infrastructure.Utilities;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);

// ========================
// Configuration
// ========================
builder.Configuration
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>();

// ========================
// Logging Configuration
// ========================
LoggingUtility.ConfigureLogging(builder, "ShareSmallBizPortal");

// ========================
// Caching Services
// ========================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<IMemoryCacheManager, MemoryCacheManager>();

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
var adminConnectionString = builder.Configuration.GetValue("ShareSmallBizUserContext", "Data Source=c:\\websites\\ShareSmallBiz\\ShareSmallBizUser.db");
builder.Services.AddDbContext<ShareSmallBizUserContext>(options =>
{
    options.UseSqlite(adminConnectionString, sqliteOptions =>
    {
        // Use split queries to avoid large, inefficient joins for multiple collection includes.
        sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
    // Configure warnings to throw an exception when a query includes multiple collections.
    options.ConfigureWarnings(warnings =>
        warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
});

// ========================
// Identity Configuration
// ========================
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

builder.Services.AddIdentity<ShareSmallBizUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
})
    .AddEntityFrameworkStores<ShareSmallBizUserContext>()
    .AddDefaultTokenProviders()
    .AddSignInManager<SignInManager<ShareSmallBizUser>>()
    .AddUserConfirmation<ShareSmallBizUser>()
    .AddDefaultUI();

// ✅ Data Protection (Ensures authentication works across restarts)
builder.Services.AddDataProtection()
    .SetApplicationName("ShareSmallBiz")
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\websites\ShareSmallBiz\keys"));

// ========================
// JWT Authentication Configuration
// ========================
// 1. Get your secret from configuration (preferably stored safely)
var jwtSecret = builder.Configuration["JwtSettings:Secret"];
var issuer = builder.Configuration["JwtSettings:Issuer"];
var audience = builder.Configuration["JwtSettings:Audience"];
var key = Encoding.ASCII.GetBytes(jwtSecret);

// 2. Register authentication and specify Bearer
builder.Services.AddAuthentication(options =>
{
})
    .AddJwtBearer(options =>
    {
        // 3. Token validation parameters
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,           // check token expiration
            ValidateIssuerSigningKey = true,   // verify signature to avoid tampering
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero          // remove default buffer time
        };

        // 4. (Optional) Define additional events for customization
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "JWT Authentication failed.");
                return Task.CompletedTask;
            }
        };
    });

// ========================
// Authorization Configuration
// ========================
builder.Services.AddAuthorization();

// ========================
// HTTP Clients
// ========================
RegisterHttpClientUtilities(builder);

// ========================
// OpenAPI/Swagger
// ========================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "ShareSmallBiz API",
        Description = "API documentation for ShareSmallBiz Portal",
        Contact = new OpenApiContact
        {
            Name = "Support",
            Email = "support@sharesmallbiz.com",
            Url = new Uri("https://sharesmallbiz.com/")
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ========================
// Application Services
// ======================
builder.Services.AddSingleton<ILogger<Program>, Logger<Program>>();
builder.Services.AddSingleton<StorageProvider, StorageProvider>();
builder.Services.AddSingleton<IStringConverter, NewtonsoftJsonStringConverter>();
builder.Services.AddSingleton(new ApplicationStatus(Assembly.GetExecutingAssembly()));

builder.Services.AddScoped<ShareSmallBizUserManager, ShareSmallBizUserManager>();

builder.Services.AddScoped<DiscussionProvider, DiscussionProvider>();
builder.Services.AddScoped<UserProvider, UserProvider>();
builder.Services.AddScoped<CommentProvider, CommentProvider>();
builder.Services.AddScoped<KeywordProvider, KeywordProvider>();


// ========================
// MVC, Razor Pages, SignalR
// ========================
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddMvc();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyHeader()
               .AllowAnyMethod()
               .SetIsOriginAllowed(_ => true)
               .AllowCredentials();
    });
});

builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
});

// ========================
// Json Serializer Configuration
// ========================
builder.Services.AddSingleton(new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
});
builder.Services.AddHttpContextAccessor();

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
app.MapControllerRoute(
    name: "areaRoute",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Logger.LogWarning("🔹 Application started successfully.");

app.UseEndpoints(endpoints =>
{
    endpoints.MapSitemap();

    // Map other endpoints like controllers, Razor Pages, etc.
    endpoints.MapRazorPages();
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});


app.Run();


static void RegisterHttpClientUtilities(WebApplicationBuilder builder)
{
    builder.Services.AddHttpClient("HttpClientService", client =>
    {
        client.Timeout = TimeSpan.FromMilliseconds(90000);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "HttpClientService");
        client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Request-Source", "HttpClientService");
    });
    // HTTP Send Service with Decorator Pattern
    builder.Services.AddSingleton(serviceProvider =>
    {
        IHttpRequestResultService baseService = new HttpRequestResultService(
            serviceProvider.GetRequiredService<ILogger<HttpRequestResultService>>(),
            serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("HttpClientDecorator"));

        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var retryOptions = configuration.GetSection("HttpRequestResultPollyOptions").Get<HttpRequestResultPollyOptions>();

        IHttpRequestResultService pollyService = new HttpRequestResultServicePolly(
            serviceProvider.GetRequiredService<ILogger<HttpRequestResultServicePolly>>(),
            baseService,
            retryOptions);

        IHttpRequestResultService telemetryService = new HttpRequestResultServiceTelemetry(
            serviceProvider.GetRequiredService<ILogger<HttpRequestResultServiceTelemetry>>(),
            pollyService);

        IHttpRequestResultService cacheService = new HttpRequestResultServiceCache(
            telemetryService,
            serviceProvider.GetRequiredService<ILogger<HttpRequestResultServiceCache>>(),
            serviceProvider.GetRequiredService<IMemoryCache>());

        return cacheService;
    });
}
