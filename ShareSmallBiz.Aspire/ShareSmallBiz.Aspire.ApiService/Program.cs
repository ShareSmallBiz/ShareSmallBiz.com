using Scalar.AspNetCore;
using ShareSmallBiz.Aspire.ApiService.WeatherService;
using ShareSmallBiz.Portal.Infrastructure.Logging;

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




// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


// Register the WeatherForecastService
builder.Services.AddSingleton<IWeatherForecastService, WeatherForecastService>();

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

app.MapGet("/weatherforecast", (IWeatherForecastService weatherService) =>
{
    return weatherService.GetForecast();
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

