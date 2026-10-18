using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using ShareSmallBiz.Core.Email;

namespace ShareSmallBiz.Api.IntegrationTests;

/// <summary>
/// Hosts the API in-memory for HTTP-level tests. Runs as "Testing" by default so only appsettings.json
/// (production-shaped settings) applies, plus a throwaway SQLite file, a random signing key,
/// a captured mailbox and a controllable clock.
/// </summary>
/// <param name="environment">"Development" exercises the local-only tooling (dev mailbox, harness).</param>
/// <param name="captureEmail">False keeps the app's own email pipeline (e.g. the dev mailbox) instead of <see cref="Emails"/>.</param>
public sealed class ApiFactory(int authPermitLimit = 10_000, string environment = "Testing", bool captureEmail = true)
    : WebApplicationFactory<Program>
{
    public string DatabasePath { get; } = Path.Combine(Path.GetTempPath(), $"ssb-api-test-{Guid.NewGuid():N}.db");

    public CapturingEmailSender Emails { get; } = new();

    public FakeTimeProvider Time { get; } = new(DateTimeOffset.UtcNow);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={DatabasePath}");
        builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("RateLimiting:AuthPermitLimit", authPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Client:BaseUrl", "https://client.test");
        // Development's "Pickup" provider would write into the developer's dev-mail folder
        builder.UseSetting("Email:Provider", "Log");

        builder.ConfigureServices(services =>
        {
            if (captureEmail)
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(Emails);
            }
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { DatabasePath, DatabasePath + "-shm", DatabasePath + "-wal" })
            File.Delete(file);
    }
}
