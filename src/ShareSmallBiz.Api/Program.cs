using Scalar.AspNetCore;
using ShareSmallBiz.Api.Auth;
using ShareSmallBiz.Api.OpenApi;
using ShareSmallBiz.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi(options =>
{
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

app.UseExceptionHandler();
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

if (!OpenApiGeneration.IsRunning && app.Configuration.GetValue("Database:MigrateOnStartup", true))
    await app.Services.MigrateDatabaseAsync();

await app.RunAsync();

/// <summary>Entry point; public so integration tests can host the app with WebApplicationFactory.</summary>
public partial class Program;
