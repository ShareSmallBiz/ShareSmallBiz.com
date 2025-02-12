using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ShareSmallBiz.Aspire.ApiService.PostService;
using ShareSmallBiz.Aspire.ApiService.WeatherService;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Logging;
using ShareSmallBiz.Portal.Infrastructure.Services;

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
LoggingUtility.ConfigureLogging(builder, "ShareSmallBiz.Aspire.ApiService");

// ========================
// Database Contexts
// ========================
// Admin User Context
var adminConnectionString = builder.Configuration.GetValue("ShareSmallBizUserContext", "Data Source=c:\\websites\\ShareSmallBiz\\ShareSmallBizUser.db");
builder.Services.AddDbContext<ShareSmallBizUserContext>(options =>
    options.UseSqlite(adminConnectionString));

// ========================
// Identity Configuration
// ========================
builder.Services.AddIdentity<ShareSmallBizUser, IdentityRole>()
    .AddEntityFrameworkStores<ShareSmallBizUserContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders()
    .AddUserManager<ApplicationUserManager>();




// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.RegisterPostProviderServices();
builder.Services.RegisterWeatherServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("ShareSmallBiz API");
    options.WithTheme(ScalarTheme.BluePlanet);
    options.WithSidebar(false);
});

app.MapPostProviderEndpoints();
app.MapWeatherEndpoints();

app.MapDefaultEndpoints();

app.Run();

