﻿using HttpClientUtility.MemoryCache;
using HttpClientUtility.RequestResult;
using HttpClientUtility.StringConverter;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using ScottPlot.Statistics;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Logging;
using ShareSmallBiz.Portal.Infrastructure.Services;
using ShareSmallBiz.Portal.Interfaces;
using ShareSmallBiz.Portal.Models;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Westwind.AspNetCore.Markdown;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;


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
    options.UseSqlite(adminConnectionString));

// ========================
// Identity Configuration
// ========================
builder.Services.AddIdentity<ShareSmallBizUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ShareSmallBizUserContext>()
    .AddDefaultTokenProviders()
    .AddSignInManager<SignInManager<ShareSmallBizUser>>()
    .AddDefaultUI();

// ✅ Data Protection (Ensures authentication works across restarts)
builder.Services.AddDataProtection()
    .SetApplicationName("ShareSmallBiz")
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\websites\ShareSmallBiz\keys"));

// ========================
// Authentication & Cookie Settings
// ========================
// ✅ Removed duplicate `AddAuthentication()` call
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.Name = ".AspNetCore.Identity.Application";

    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = async context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userPrincipal = context.Principal;

            if (userPrincipal.Identity.IsAuthenticated)
            {
                logger.LogInformation("🔹 Authentication cookie validated successfully for {UserName}", userPrincipal.Identity.Name);
            }
            else
            {
                logger.LogWarning("⚠️ Authentication cookie validation failed! User is NOT authenticated.");
            }
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
            Url = new Uri("https://sharesmallbiz.com/contact")
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
// ========================
builder.Services.AddSingleton<IStorageProvider, StorageProvider>();
builder.Services.AddScoped<PostProvider, PostProvider>();
builder.Services.AddScoped<UserService, UserService>();
builder.Services.AddScoped<CommentProvider, CommentProvider>();
builder.Services.AddSingleton<IStringConverter, NewtonsoftJsonStringConverter>();
builder.Services.AddSingleton(new ApplicationStatus(Assembly.GetExecutingAssembly()));
builder.Services.AddSingleton<ChatHistoryStore>();
builder.Services.AddSingleton<IScopeInformation, ScopeInformation>();

// ========================
// Markdown Support
// ========================
builder.Services.AddMarkdown();

// ========================
// MVC, Razor Pages, SignalR
// ========================
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddMvc().AddApplicationPart(typeof(MarkdownPageProcessorMiddleware).Assembly);

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
// OpenAI Chat Completion Service
// ========================
string apikey = builder.Configuration.GetValue<string>("OPENAI_API_KEY") ?? "not found";
string modelId = builder.Configuration.GetValue<string>("MODEL_ID") ?? "gpt-4o";
builder.Services.AddOpenAIChatCompletion(modelId, apikey);

// ========================
// Json Serializer Configuration
// ========================
builder.Services.AddSingleton(new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
});

var app = builder.Build();

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
        app.Logger.LogWarning("⚠️ Middleware: User is NOT authenticated.");
    }

    var authCookie = context.Request.Cookies[".AspNetCore.Identity.Application"];
    if (authCookie != null)
    {
        app.Logger.LogInformation("🔹 Auth Cookie Found in Request");
    }
    else
    {
        app.Logger.LogWarning("⚠️ No Authentication Cookie Found in Request!");
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
