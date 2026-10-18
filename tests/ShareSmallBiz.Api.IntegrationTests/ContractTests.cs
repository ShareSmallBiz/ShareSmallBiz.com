using System.Text.Json;

namespace ShareSmallBiz.Api.IntegrationTests;

/// <summary>Guards on the published OpenAPI contract itself.</summary>
[TestClass]
public sealed class ContractTests
{
    /// <summary>
    /// Schemas allowed to carry an email address: auth requests and the caller's own account.
    /// Anything else exposing "email" (e.g. an author on a post) is a privacy leak.
    /// </summary>
    private static readonly HashSet<string> EmailAllowed =
    [
        "RegisterRequest", "LoginRequest", "EmailRequest", "ResetPasswordRequest", "CurrentUserResponse",
    ];

    private static readonly string[] PrivateFieldNames =
        ["email", "passwordHash", "securityStamp", "lockoutEnd", "accessFailedCount", "phoneNumber", "normalizedEmail"];

    [TestMethod]
    public async Task OnlyAllowlistedSchemas_ExposePrivateAccountFields()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var doc = JsonDocument.Parse(await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative)));

        var leaks = new List<string>();
        foreach (var schema in doc.RootElement.GetProperty("components").GetProperty("schemas").EnumerateObject())
        {
            if (!schema.Value.TryGetProperty("properties", out var properties))
                continue;
            foreach (var field in PrivateFieldNames)
            {
                var allowed = field == "email" && EmailAllowed.Contains(schema.Name);
                if (properties.TryGetProperty(field, out _) && !allowed)
                    leaks.Add($"{schema.Name}.{field}");
            }
        }

        Assert.IsEmpty(leaks, "Private fields exposed: " + string.Join(", ", leaks));
    }

    [TestMethod]
    public async Task ServedContract_MatchesCommittedContract()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var served = await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        var committed = await File.ReadAllTextAsync(Path.Combine(FindRepoRoot(), "openapi", "ShareSmallBiz.Api.json"));

        Assert.AreEqual(Normalize(committed), Normalize(served), "Run a build and commit openapi/ShareSmallBiz.Api.json.");
    }

    /// <summary>Compact form without "servers", which the live document fills from the request host.</summary>
    private static string Normalize(string json)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
        node.Remove("servers");
        return node.ToJsonString();
    }

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "global.json")))
                return dir.FullName;
        }
        throw new InvalidOperationException("Repository root (global.json) not found.");
    }
}
