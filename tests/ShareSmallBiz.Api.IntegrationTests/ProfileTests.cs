using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ShareSmallBiz.Api.Auth;
using ShareSmallBiz.Api.Common;
using ShareSmallBiz.Api.Profiles;

namespace ShareSmallBiz.Api.IntegrationTests;

[TestClass]
public sealed class ProfileTests
{
    /// <summary>Exactly what the public may see; adding a field here is a deliberate privacy decision.</summary>
    private static readonly string[] PublicProfileFields = ["userName", "displayName", "bio", "websiteUrl", "joinedAt"];

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

    private static async Task<(string UserName, AuthTokensResponse Tokens)> NewMemberAsync(string displayName = "Test Member")
    {
        var userName = AuthClient.NewUserName();
        var tokens = await AuthClient.RegisterConfirmAndLoginAsync(_client, _factory, AuthClient.NewEmail(), userName, displayName);
        return (userName, tokens);
    }

    private static Task<HttpResponseMessage> UpdateProfileAsync(string accessToken, object body) =>
        AuthClient.SendAsync(_client, HttpMethod.Put, "/api/v1/me/profile", accessToken, body);

    // ── Public profile ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetProfile_Public_ReturnsOnlyPublicFields()
    {
        var (userName, tokens) = await NewMemberAsync();
        (await UpdateProfileAsync(tokens.AccessToken, new { displayName = "Acme Bakery", firstName = "Ada", lastName = "Lovelace", bio = "Bread daily.", websiteUrl = "https://acme.example" }))
            .EnsureSuccessStatusCode();

        var response = await _client.GetAsync(new Uri($"/api/v1/profiles/{userName}", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var names = json.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
        CollectionAssert.AreEquivalent(PublicProfileFields, names);
        Assert.AreEqual("Acme Bakery", json.RootElement.GetProperty("displayName").GetString());
    }

    [TestMethod]
    public async Task GetProfile_UnknownUser_Returns404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/profiles/nobody-here", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetProfile_IsCaseInsensitive()
    {
        var (userName, _) = await NewMemberAsync();

        var response = await _client.GetAsync(new Uri($"/api/v1/profiles/{userName.ToUpperInvariant()}", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetProfile_Unconfirmed_Returns404()
    {
        var userName = AuthClient.NewUserName();
        (await AuthClient.RegisterAsync(_client, AuthClient.NewEmail(), userName)).EnsureSuccessStatusCode();

        var response = await _client.GetAsync(new Uri($"/api/v1/profiles/{userName}", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetProfile_Private_HiddenFromOthers_VisibleToOwner()
    {
        var (userName, owner) = await NewMemberAsync();
        var (_, other) = await NewMemberAsync();
        (await UpdateProfileAsync(owner.AccessToken, new { displayName = "Quiet Shop", profileVisibility = "Private" })).EnsureSuccessStatusCode();
        var path = $"/api/v1/profiles/{userName}";

        var anonymous = await _client.GetAsync(new Uri(path, UriKind.Relative));
        var otherMember = await AuthClient.SendAsync(_client, HttpMethod.Get, path, other.AccessToken);
        var self = await AuthClient.SendAsync(_client, HttpMethod.Get, path, owner.AccessToken);

        Assert.AreEqual(HttpStatusCode.NotFound, anonymous.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, otherMember.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, self.StatusCode);
    }

    // ── Directory ────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task ListProfiles_SearchAndPaging_ReturnsOnlyPublicConfirmedMembers()
    {
        var tag = $"Dir{Guid.NewGuid():N}"[..12];
        for (var i = 0; i < 3; i++)
        {
            _factory.Time.Advance(TimeSpan.FromSeconds(1)); // distinct join times for the ordering assertion
            await NewMemberAsync($"{tag} Shop {i}");
        }
        var (_, hidden) = await NewMemberAsync($"{tag} Hidden");
        (await UpdateProfileAsync(hidden.AccessToken, new { displayName = $"{tag} Hidden", profileVisibility = "Private" })).EnsureSuccessStatusCode();
        (await AuthClient.RegisterAsync(_client, AuthClient.NewEmail(), displayName: $"{tag} Unconfirmed")).EnsureSuccessStatusCode();

        var page1 = await _client.GetFromJsonAsync<PagedResponse<PublicProfileResponse>>($"/api/v1/profiles?search={tag}&pageSize=2");
        var page2 = await _client.GetFromJsonAsync<PagedResponse<PublicProfileResponse>>($"/api/v1/profiles?search={tag}&pageSize=2&page=2");

        Assert.AreEqual(3, page1!.TotalCount);
        Assert.AreEqual(2, page1.TotalPages);
        Assert.HasCount(2, page1.Items);
        Assert.HasCount(1, page2!.Items);
        Assert.AreEqual($"{tag} Shop 2", page1.Items[0].DisplayName, "newest first");
        Assert.IsFalse(page1.Items.Concat(page2.Items).Any(p => p.DisplayName.Contains("Hidden", StringComparison.Ordinal) || p.DisplayName.Contains("Unconfirmed", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task ListProfiles_SearchWildcardsAreLiteral()
    {
        var response = await _client.GetFromJsonAsync<PagedResponse<PublicProfileResponse>>("/api/v1/profiles?search=%25");

        Assert.AreEqual(0, response!.TotalCount, "a '%' search must not match everything");
    }

    [TestMethod]
    [DataRow("page=0")]
    [DataRow("pageSize=0")]
    [DataRow("pageSize=51")]
    public async Task ListProfiles_InvalidPaging_Returns400(string query)
    {
        var response = await _client.GetAsync(new Uri($"/api/v1/profiles?{query}", UriKind.Relative));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Editing your own profile ─────────────────────────────────────────────

    [TestMethod]
    public async Task UpdateProfile_WithoutToken_Returns401()
    {
        var response = await UpdateProfileAsync(accessToken: null!, new { displayName = "Nope" });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProfile_PersistsAndIsReturnedByMe()
    {
        var (_, tokens) = await NewMemberAsync();

        var update = await UpdateProfileAsync(tokens.AccessToken, new { displayName = "  Corner Store  ", firstName = "Grace", bio = "Open 7-7.", websiteUrl = "https://corner.example/" });
        var me = await (await AuthClient.GetMeAsync(_client, tokens.AccessToken)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.AreEqual(HttpStatusCode.OK, update.StatusCode);
        Assert.AreEqual("Corner Store", me.GetProperty("displayName").GetString(), "trimmed");
        Assert.AreEqual("Grace", me.GetProperty("firstName").GetString());
        Assert.AreEqual("Open 7-7.", me.GetProperty("bio").GetString());
        Assert.AreEqual("Public", me.GetProperty("profileVisibility").GetString(), "enums are strings in the contract");
    }

    [TestMethod]
    [DataRow("javascript:alert(1)")]
    [DataRow("ftp://files.example")]
    [DataRow("/relative/path")]
    public async Task UpdateProfile_UnsafeWebsiteUrl_Returns400(string url)
    {
        var (_, tokens) = await NewMemberAsync();

        var response = await UpdateProfileAsync(tokens.AccessToken, new { displayName = "Shop", websiteUrl = url });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProfile_MissingDisplayName_Returns400()
    {
        var (_, tokens) = await NewMemberAsync();

        var response = await UpdateProfileAsync(tokens.AccessToken, new { bio = "no name" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
