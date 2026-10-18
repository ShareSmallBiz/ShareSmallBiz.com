using System.Net;
using System.Text.Json;

namespace ShareSmallBiz.Api.IntegrationTests;

/// <summary>Cross-cutting behavior every endpoint relies on: health, contract, errors, CORS.</summary>
[TestClass]
public sealed class PlatformTests
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

    [TestMethod]
    public async Task Health_ReportsHealthy()
    {
        var response = await _client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("Healthy", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task OpenApiDocument_IsServed()
    {
        var response = await _client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsTrue(doc.RootElement.TryGetProperty("openapi", out _));
    }

    [TestMethod]
    public async Task UnknownRoute_ReturnsProblemDetails404_NotRedirect()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/does-not-exist", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    [DataRow("https://sharesmallbiz.com")]
    [DataRow("https://www.sharesmallbiz.com")]
    public async Task Cors_AllowsConfiguredOrigins(string origin)
    {
        var response = await SendPreflightAsync(origin);

        Assert.IsTrue(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.AreEqual(origin, values.Single());
        Assert.IsFalse(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [TestMethod]
    public async Task Cors_RejectsUnknownOrigin()
    {
        var response = await SendPreflightAsync("https://evil.example");

        Assert.IsFalse(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static async Task<HttpResponseMessage> SendPreflightAsync(string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, new Uri("/health", UriKind.Relative));
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        return await _client.SendAsync(request);
    }
}
