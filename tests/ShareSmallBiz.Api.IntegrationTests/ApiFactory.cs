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
/// Hosts the API in-memory for HTTP-level tests. Runs as "Testing" so only appsettings.json
/// (production-shaped settings) applies, plus a throwaway SQLite file, a random signing key,
/// a captured mailbox and a controllable clock.
/// </summary>
public sealed class ApiFactory(int authPermitLimit = 10_000) : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"ssb-api-test-{Guid.NewGuid():N}.db");

    public CapturingEmailSender Emails { get; } = new();

    public FakeTimeProvider Time { get; } = new(DateTimeOffset.UtcNow);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
        builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("RateLimiting:AuthPermitLimit", authPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Client:BaseUrl", "https://client.test");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
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
        foreach (var file in new[] { _databasePath, _databasePath + "-shm", _databasePath + "-wal" })
            File.Delete(file);
    }
}
