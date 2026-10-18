using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ShareSmallBiz.Api.IntegrationTests;

/// <summary>
/// Hosts the API in-memory for HTTP-level tests. Runs as "Testing" so only appsettings.json
/// (production-shaped settings) applies, never developer overrides.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
