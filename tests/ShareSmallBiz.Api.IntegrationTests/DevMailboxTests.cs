using System.Net;
using System.Net.Http.Json;
using ShareSmallBiz.Api.Development;

namespace ShareSmallBiz.Api.IntegrationTests;

/// <summary>The dev mailbox lets a browser-only flow (e.g. API Test Spark) finish sign-up locally; it must never exist elsewhere.</summary>
[TestClass]
public sealed class DevMailboxTests
{
    private static ApiFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        _factory = new ApiFactory(environment: "Development", captureEmail: false);
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public void DevelopmentTests_UseTheThrowawayDatabase_NotTheDevelopersDatabase()
    {
        Assert.IsTrue(File.Exists(_factory.DatabasePath), "test settings must override appsettings.Development.json");
    }

    [TestMethod]
    public async Task SignUpFlow_CompletesUsingOnlyTheApi()
    {
        var email = AuthClient.NewEmail();
        (await AuthClient.RegisterAsync(_client, email)).EnsureSuccessStatusCode();

        var link = (await LatestAsync(email, "confirm"))!.Links.Single();
        var confirm = await _client.PostAsJsonAsync("/api/v1/auth/confirm-email", new { userId = link.Query["userId"], token = link.Query["token"] });
        var login = await AuthClient.LoginAsync(_client, email);

        Assert.AreEqual("/confirm-email", link.Path);
        Assert.AreEqual(HttpStatusCode.NoContent, confirm.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
    }

    [TestMethod]
    public async Task PasswordResetFlow_CompletesUsingOnlyTheApi()
    {
        var email = AuthClient.NewEmail();
        (await AuthClient.RegisterAsync(_client, email)).EnsureSuccessStatusCode();
        var confirmLink = (await LatestAsync(email, "confirm"))!.Links.Single();
        (await _client.PostAsJsonAsync("/api/v1/auth/confirm-email", new { userId = confirmLink.Query["userId"], token = confirmLink.Query["token"] })).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email })).EnsureSuccessStatusCode();

        var resetLink = (await LatestAsync(email, "reset"))!.Links.Single();
        var reset = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email = resetLink.Query["email"], token = resetLink.Query["token"], newPassword = "a brand new passphrase" });

        Assert.AreEqual(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await AuthClient.LoginAsync(_client, email, "a brand new passphrase")).StatusCode);
    }

    [TestMethod]
    public async Task Latest_FiltersByRecipient_AndReturns404WhenNothingMatches()
    {
        var response = await _client.GetAsync(new Uri($"/dev/mailbox/latest?to={Uri.EscapeDataString(AuthClient.NewEmail())}", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Mailbox_IsInTheServedDevelopmentContract_SoTheHarnessCanFindIt()
    {
        var contract = await _client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.Contains("/dev/mailbox/latest", contract);
    }

    [TestMethod]
    public async Task Mailbox_IsAbsentOutsideDevelopment()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var latest = await client.GetAsync(new Uri("/dev/mailbox/latest", UriKind.Relative));
        var list = await client.GetAsync(new Uri("/dev/mailbox", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.NotFound, latest.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, list.StatusCode);
    }

    private static Task<DevMailMessage?> LatestAsync(string email, string subject) =>
        _client.GetFromJsonAsync<DevMailMessage>($"/dev/mailbox/latest?to={Uri.EscapeDataString(email)}&subject={subject}");
}
