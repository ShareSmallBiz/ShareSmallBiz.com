using System.Text.Json.Serialization;
using ApiTestSpark;
using Scalar.AspNetCore;
using ShareSmallBiz.Api.Auth;
using ShareSmallBiz.Api.OpenApi;
using ShareSmallBiz.Api.Profiles;
using ShareSmallBiz.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddValidation();
builder.Services.AddOpenApi(options =>
{
    // The Development-only test harness is tooling, not API: keep it out of the contract in every environment
    options.ShouldInclude = description =>
        description.RelativePath?.StartsWith("api-test-spark", StringComparison.OrdinalIgnoreCase) != true;
    options.AddDocumentTransformer<BearerSecurityTransformer>();
    options.AddOperationTransformer<BearerSecurityTransformer>();
});
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(validateOnStart: !OpenApiGeneration.IsRunning);
builder.Services.AddApiAuth(builder.Configuration);

// Thin clients authenticate with bearer tokens, not cookies, so credentials are not allowed
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Development rethrows malformed-request errors as BadHttpRequestException; keep their 4xx status instead of a 500
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// The API is public: its contract and reference docs are served in every environment
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("ShareSmallBiz API"));
app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapProfileEndpoints();

// Interactive test harness at /api-test-spark/ for local validation; Development only, never on a public server
app.MapApiTestSpark(options =>
{
    options.OpenApiUrl = "/openapi/v1.json";
    options.AuthScheme = AuthConstants.BearerSchemeName;
    options.Environments = ["Development"];
    options.EnableDemoIntegrations = false;
});

if (!OpenApiGeneration.IsRunning && app.Configuration.GetValue("Database:MigrateOnStartup", true))
    await app.Services.MigrateDatabaseAsync();

await app.RunAsync();

/// <summary>Entry point; public so integration tests can host the app with WebApplicationFactory.</summary>
public partial class Program;
