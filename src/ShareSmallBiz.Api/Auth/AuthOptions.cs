using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ShareSmallBiz.Api.Auth;

/// <summary>JWT access-token and refresh-token settings (section <c>Jwt</c>).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "https://api.sharesmallbiz.com";

    public string Audience { get; set; } = "sharesmallbiz";

    /// <summary>HMAC-SHA256 key, at least 32 bytes. Never in appsettings: user secrets or <c>Jwt__SigningKey</c>.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 14;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}

internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
            return ValidateOptionsResult.Fail("Jwt:SigningKey must be configured with at least 32 bytes (user secrets locally, Jwt__SigningKey on the server).");
        if (options.AccessTokenMinutes <= 0 || options.RefreshTokenDays <= 0)
            return ValidateOptionsResult.Fail("Jwt token lifetimes must be positive.");
        return ValidateOptionsResult.Success;
    }
}

/// <summary>The thin client that email links point at (section <c>Client</c>).</summary>
public sealed class ClientOptions
{
    public const string SectionName = "Client";

    public Uri BaseUrl { get; set; } = new("https://sharesmallbiz.com");
}

/// <summary>Per-IP limits for anonymous auth endpoints (section <c>RateLimiting</c>).</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public const string AuthPolicy = "auth";

    public int AuthPermitLimit { get; set; } = 10;

    public int AuthWindowSeconds { get; set; } = 60;
}
