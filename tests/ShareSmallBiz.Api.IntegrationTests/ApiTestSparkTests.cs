using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ShareSmallBiz.Api.IntegrationTests;

/// <summary>The API Test Spark harness is a local validation tool: on in Development, absent everywhere else.</summary>
[TestClass]
public sealed class ApiTestSparkTests
{
    [TestMethod]
    public async Task Harness_IsServedInDevelopment_AndPointsAtOurContract()
    {
        using var factory = new DevelopmentFactory();
        using var client = factory.CreateClient();

        var page = await client.GetAsync(new Uri("/api-test-spark/", UriKind.Relative));
        var config = await client.GetStringAsync(new Uri("/api-test-spark/config", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("/openapi/v1.json", config);
    }

    [TestMethod]
    public async Task Harness_NeverAppearsInTheApiContract()
    {
        // Build-time generation can run under ASPNETCORE_ENVIRONMENT=Development (e.g. api-local.ps1)
        using var factory = new DevelopmentFactory();
        using var client = factory.CreateClient();

        var contract = await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.DoesNotContain("api-test-spark", contract);
    }

    [TestMethod]
    public async Task Harness_IsAbsentOutsideDevelopment()
    {
        using var factory = new ApiFactory(); // "Testing": production-shaped settings
        using var client = factory.CreateClient();

        var page = await client.GetAsync(new Uri("/api-test-spark/", UriKind.Relative));
        var config = await client.GetAsync(new Uri("/api-test-spark/config", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.NotFound, page.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, config.StatusCode);
    }

    /// <summary>Development settings, but no database and no dependency on the developer's user secrets.</summary>
    private sealed class DevelopmentFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        }
    }
}
