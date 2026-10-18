using System.Net.Http.Headers;
using System.Net.Http.Json;
using ShareSmallBiz.Api.Auth;

namespace ShareSmallBiz.Api.IntegrationTests;

/// <summary>Thin helpers over the auth endpoints, used to arrange test state over real HTTP.</summary>
internal static class AuthClient
{
    public const string Password = "correct horse battery";

    public static string NewEmail() => $"user-{Guid.NewGuid():N}@example.test";

    public static string NewUserName() => $"user-{Guid.NewGuid():N}"[..20];

    public static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client, string email, string? userName = null, string password = Password, string displayName = "Test Member") =>
        client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            userName = userName ?? NewUserName(),
            displayName,
        });

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    /// <summary>Registers, follows the confirmation link and signs in; returns the issued tokens.</summary>
    public static async Task<AuthTokensResponse> RegisterConfirmAndLoginAsync(
        HttpClient client, ApiFactory factory, string email, string? userName = null, string displayName = "Test Member")
    {
        (await RegisterAsync(client, email, userName, displayName: displayName)).EnsureSuccessStatusCode();
        await ConfirmAsync(client, factory, email);
        var login = await LoginAsync(client, email);
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<AuthTokensResponse>())!;
    }

    public static async Task ConfirmAsync(HttpClient client, ApiFactory factory, string email)
    {
        var query = factory.Emails.LastLinkQuery(email, out _);
        var confirm = await client.PostAsJsonAsync("/api/v1/auth/confirm-email", new { userId = query["userId"], token = query["token"] });
        confirm.EnsureSuccessStatusCode();
    }

    public static Task<HttpResponseMessage> GetMeAsync(HttpClient client, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, string? accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> RefreshAsync(HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
}
