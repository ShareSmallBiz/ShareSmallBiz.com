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

    /// <summary>
    /// Conventions that make the docs and API Test Spark useful: every operation is named (stable
    /// operationId for generated clients), summarized, described, and grouped under a "Group: Label" tag.
    /// </summary>
    [TestMethod]
    public async Task EveryOperation_IsNamedSummarizedDescribedAndGrouped()
    {
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(CommittedContractPath()));

        var problems = new List<string>();
        foreach (var path in doc.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                var name = $"{operation.Name.ToUpperInvariant()} {path.Name}";
                var op = operation.Value;
                if (!op.TryGetProperty("operationId", out _)) problems.Add($"{name}: no operationId (.WithName)");
                if (!op.TryGetProperty("summary", out _)) problems.Add($"{name}: no summary (.WithSummary)");
                if (!op.TryGetProperty("description", out _)) problems.Add($"{name}: no description (.WithDescription)");
                if (!op.TryGetProperty("tags", out var tags) || !tags.EnumerateArray().All(t => t.GetString()!.Contains(": ", StringComparison.Ordinal)))
                    problems.Add($"{name}: tags must be \"Group: Label\"");
            }
        }

        Assert.IsEmpty(problems, string.Join(Environment.NewLine, problems));
    }

    [TestMethod]
    public async Task CommittedContract_ContainsNoLocalOnlyTooling()
    {
        var committed = await File.ReadAllTextAsync(CommittedContractPath());

        Assert.DoesNotContain("\"/dev/", committed);
        Assert.DoesNotContain("api-test-spark", committed);
    }

    private static string CommittedContractPath() => Path.Combine(FindRepoRoot(), "openapi", "ShareSmallBiz.Api.json");

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
