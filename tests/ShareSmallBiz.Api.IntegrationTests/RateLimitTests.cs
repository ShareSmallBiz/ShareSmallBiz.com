using System.Net;

namespace ShareSmallBiz.Api.IntegrationTests;

[TestClass]
public sealed class RateLimitTests
{
    [TestMethod]
    public async Task AuthEndpoints_OverLimit_Return429Problem()
    {
        using var factory = new ApiFactory(authPermitLimit: 3);
        using var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await AuthClient.LoginAsync(client, AuthClient.NewEmail())).StatusCode);
        var limited = await AuthClient.LoginAsync(client, AuthClient.NewEmail());

        Assert.AreEqual(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.AreEqual("application/problem+json", limited.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task NonAuthEndpoints_AreNotLimited()
    {
        using var factory = new ApiFactory(authPermitLimit: 1);
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
            Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(new Uri("/health", UriKind.Relative))).StatusCode);
    }
}
