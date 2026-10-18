using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace ShareSmallBiz.Api.IntegrationTests;

/// <summary>A misconfigured server must refuse to boot rather than fail on a user's first request.</summary>
[TestClass]
public sealed class StartupValidationTests
{
    private const string ValidKey = "0123456789abcdef0123456789abcdef0123456789abcdef";

    [TestMethod]
    public void Production_WithoutSigningKey_FailsToStart() =>
        AssertStartupFails<OptionsValidationException>(new() { ["Email:Provider"] = "Log" }, "Jwt:SigningKey");

    [TestMethod]
    public void Production_WithShortSigningKey_FailsToStart() =>
        AssertStartupFails<OptionsValidationException>(new() { ["Jwt:SigningKey"] = "too-short" }, "Jwt:SigningKey");

    [TestMethod]
    public void Production_WithoutEmailProvider_FailsToStart() =>
        AssertStartupFails<OptionsValidationException>(new() { ["Jwt:SigningKey"] = ValidKey }, "Email:Provider");

    [TestMethod]
    public void Production_WithLogEmailProvider_FailsToStart() =>
        AssertStartupFails<OptionsValidationException>(new() { ["Jwt:SigningKey"] = ValidKey, ["Email:Provider"] = "Log" }, "only allowed in Development");

    [TestMethod]
    public void Production_WithoutConnectionString_FailsToStart() =>
        AssertStartupFails<InvalidOperationException>(new() { ["Database:MigrateOnStartup"] = null }, "ConnectionStrings:Default", withDatabase: false);

    private static void AssertStartupFails<TException>(Dictionary<string, string?> settings, string expectedMessage, bool withDatabase = true)
        where TException : Exception
    {
        using var factory = new ProductionFactory(settings, withDatabase);

        // Several invalid settings are reported together as an AggregateException
        var thrown = Assert.Throws<Exception>(() => factory.CreateClient());
        var failures = thrown is AggregateException aggregate ? aggregate.Flatten().InnerExceptions : [thrown];

        Assert.IsTrue(
            failures.OfType<TException>().Any(e => e.Message.Contains(expectedMessage, StringComparison.Ordinal)),
            $"Expected {typeof(TException).Name} mentioning '{expectedMessage}', got: {thrown}");
    }

    private sealed class ProductionFactory(Dictionary<string, string?> settings, bool withDatabase) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            // Validation runs at host start; skip the database unless the test is about it
            builder.UseSetting("Database:MigrateOnStartup", withDatabase ? "false" : "true");
            foreach (var (key, value) in settings.Where(s => s.Value is not null))
                builder.UseSetting(key, value);
        }
    }
}
