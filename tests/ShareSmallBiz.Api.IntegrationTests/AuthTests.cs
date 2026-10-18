using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ShareSmallBiz.Api.Auth;

namespace ShareSmallBiz.Api.IntegrationTests;

[TestClass]
public sealed class AuthTests
{
    private static ApiFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        _factory = new ApiFactory();
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ── Registration ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Register_ValidRequest_Returns202AndSendsConfirmationLinkToClient()
    {
        var email = AuthClient.NewEmail();

        var response = await AuthClient.RegisterAsync(_client, email);

        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        var query = _factory.Emails.LastLinkQuery(email, out var link);
        Assert.AreEqual("https://client.test/confirm-email", link.GetLeftPart(UriPartial.Path));
        Assert.IsTrue(query.ContainsKey("userId") && query.ContainsKey("token"));
    }

    [TestMethod]
    [DataRow("not-an-email", "valid-name", "long enough password")]
    [DataRow("ok@example.test", "Bad Name!", "long enough password")]
    [DataRow("ok@example.test", "ab", "long enough password")]
    [DataRow("ok@example.test", "-leading-hyphen", "long enough password")]
    [DataRow("ok@example.test", "valid-name", "short")]
    public async Task Register_InvalidInput_Returns400ValidationProblem(string email, string userName, string password)
    {
        var response = await AuthClient.RegisterAsync(_client, email, userName, password);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsTrue(problem.RootElement.TryGetProperty("errors", out _));
    }

    [TestMethod]
    public async Task Register_ExistingEmail_Returns202AndEmailsOwnerInsteadOfRevealingAccount()
    {
        var email = AuthClient.NewEmail();
        (await AuthClient.RegisterAsync(_client, email)).EnsureSuccessStatusCode();

        var second = await AuthClient.RegisterAsync(_client, email);

        Assert.AreEqual(HttpStatusCode.Accepted, second.StatusCode);
        var sent = _factory.Emails.SentTo(email);
        Assert.HasCount(2, sent);
        Assert.Contains("already have", sent[^1].Subject);
    }

    [TestMethod]
    public async Task Register_TakenUserName_Returns400()
    {
        var userName = AuthClient.NewUserName();
        (await AuthClient.RegisterAsync(_client, AuthClient.NewEmail(), userName)).EnsureSuccessStatusCode();

        var response = await AuthClient.RegisterAsync(_client, AuthClient.NewEmail(), userName);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Confirmation and login ───────────────────────────────────────────────

    [TestMethod]
    public async Task Login_BeforeEmailConfirmed_Returns403WithCode()
    {
        var email = AuthClient.NewEmail();
        (await AuthClient.RegisterAsync(_client, email)).EnsureSuccessStatusCode();

        var response = await AuthClient.LoginAsync(_client, email);

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual("email_not_confirmed", problem.RootElement.GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task ConfirmEmail_TamperedToken_Returns400()
    {
        var email = AuthClient.NewEmail();
        (await AuthClient.RegisterAsync(_client, email)).EnsureSuccessStatusCode();
        var query = _factory.Emails.LastLinkQuery(email, out _);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/confirm-email", new { userId = query["userId"], token = "AAAA" + query["token"] });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task FullFlow_RegisterConfirmLogin_MeReturnsOwnPrivateAccount()
    {
        var email = AuthClient.NewEmail();

        var tokens = await AuthClient.RegisterConfirmAndLoginAsync(_client, _factory, email);
        var me = await AuthClient.GetMeAsync(_client, tokens.AccessToken);

        Assert.AreEqual("Bearer", tokens.TokenType);
        Assert.AreEqual(HttpStatusCode.OK, me.StatusCode);
        var account = (await me.Content.ReadFromJsonAsync<CurrentUserResponse>())!;
        Assert.AreEqual(email, account.Email);
        Assert.IsTrue(account.EmailConfirmed);
        Assert.AreEqual("Test Member", account.DisplayName);
    }

    [TestMethod]
    public async Task Login_UnknownEmailAndWrongPassword_ReturnSameGeneric401()
    {
        var email = AuthClient.NewEmail();
        await AuthClient.RegisterConfirmAndLoginAsync(_client, _factory, email);

        var unknown = await AuthClient.LoginAsync(_client, AuthClient.NewEmail());
        var wrong = await AuthClient.LoginAsync(_client, email, "wrong password!!");

        Assert.AreEqual(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, wrong.StatusCode);
        using var unknownProblem = JsonDocument.Parse(await unknown.Content.ReadAsStringAsync());
        using var wrongProblem = JsonDocument.Parse(await wrong.Content.ReadAsStringAsync());
        Assert.AreEqual(unknownProblem.RootElement.GetProperty("title").GetString(), wrongProblem.RootElement.GetProperty("title").GetString());
    }

    [TestMethod]
    public async Task Login_FiveFailures_LocksAccountEvenForCorrectPassword()
    {
        var email = AuthClient.NewEmail();
        await AuthClient.RegisterConfirmAndLoginAsync(_client, _factory, email);

        for (var i = 0; i < 5; i++)
            await AuthClient.LoginAsync(_client, email, "wrong password!!");
        var correct = await AuthClient.LoginAsync(_client, email);

        Assert.AreEqual(HttpStatusCode.Unauthorized, correct.StatusCode);
    }

    [TestMethod]
    public async Task Me_WithoutToken_Returns401Problem()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/me", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task Me_WithForgedToken_Returns401()
    {
        var tokens = await AuthClient.RegisterConfirmAndLoginAsync(_client, _factory, AuthClient.NewEmail());
        var parts = tokens.AccessToken.Split('.');
        var forged = $"{parts[0]}.{parts[1]}.{new string('A', parts[2].Length)}";

        var response = await AuthClient.GetMeAsync(_client, forged);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Refresh tokens ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task Refresh_RotatesTokens_AndNewAccessTokenWorks()
    {
        var tokens = await AuthClient.RegisterConfirmAndLoginAsync(_client, _factory, AuthClient.NewEmail());

        var response = await AuthClient.RefreshAsync(_client, tokens.RefreshToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var rotated = (await response.Content.ReadFromJsonAsync<AuthTokensResponse>())!;
        Assert.AreNotEqual(tokens.RefreshToken, rotated.RefreshToken);
        Assert.AreEqual(HttpStatusCode.OK, (await AuthClient.GetMeAsync(_client, rotated.AccessToken)).StatusCode);
    }

    [TestMethod]
    public async Task Refresh_ReusingSpentToken_RevokesWholeSession()
    {
        var tokens = await AuthClient.RegisterConfirmAndLoginAsync(_client, _factory, AuthClient.NewEmail());
        var rotated = (await (await AuthClient.RefreshAsync(_client, tokens.RefreshToken)).Content.ReadFromJsonAsync<AuthTokensResponse>())!;

        var replay = await AuthClient.RefreshAsync(_client, tokens.RefreshToken);
        var afterReplay = await AuthClient.RefreshAsync(_client, rotated.RefreshToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, afterReplay.StatusCode, "the legitimate newer token must die with its family");
    }

    [TestMethod]
    public async Task Refresh_UnknownToken_Returns401()
    {
        var response = await AuthClient.RefreshAsync(_client, "not-a-real-token");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Refresh_AfterExpiry_Returns401()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var tokens = await AuthClient.RegisterConfirmAndLoginAsync(client, factory, AuthClient.NewEmail());

        factory.Time.Advance(TimeSpan.FromDays(15));
        var response = await AuthClient.RefreshAsync(client, tokens.RefreshToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Logout_RevokesRefreshToken_OtherSessionsSurvive()
    {
        var email = AuthClient.NewEmail();
        var sessionA = await AuthClient.RegisterConfirmAndLoginAsync(_client, _factory, email);
        var sessionB = (await (await AuthClient.LoginAsync(_client, email)).Content.ReadFromJsonAsync<AuthTokensResponse>())!;

        var logout = await _client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = sessionA.RefreshToken });

        Assert.AreEqual(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await AuthClient.RefreshAsync(_client, sessionA.RefreshToken)).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await AuthClient.RefreshAsync(_client, sessionB.RefreshToken)).StatusCode);
    }

    // ── Password reset ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task ForgotPassword_UnknownEmail_Returns202AndSendsNothing()
    {
        var email = AuthClient.NewEmail();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });

        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        Assert.IsEmpty(_factory.Emails.SentTo(email));
    }

    [TestMethod]
    public async Task ResetPassword_FullFlow_ChangesPasswordAndSignsOutEverySession()
    {
        var email = AuthClient.NewEmail();
        var tokens = await AuthClient.RegisterConfirmAndLoginAsync(_client, _factory, email);
        (await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email })).EnsureSuccessStatusCode();
        var query = _factory.Emails.LastLinkQuery(email, out var link);
        const string newPassword = "a brand new passphrase";

        var reset = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email = query["email"], token = query["token"], newPassword });

        Assert.AreEqual("https://client.test/reset-password", link.GetLeftPart(UriPartial.Path));
        Assert.AreEqual(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await AuthClient.LoginAsync(_client, email)).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await AuthClient.LoginAsync(_client, email, newPassword)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await AuthClient.RefreshAsync(_client, tokens.RefreshToken)).StatusCode);
    }

    [TestMethod]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        var email = AuthClient.NewEmail();
        await AuthClient.RegisterConfirmAndLoginAsync(_client, _factory, email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email, token = "bm90LWEtdG9rZW4", newPassword = "a brand new passphrase" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
