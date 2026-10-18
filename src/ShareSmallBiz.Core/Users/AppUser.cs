using Microsoft.AspNetCore.Identity;

namespace ShareSmallBiz.Core.Users;

/// <summary>
/// A member account. <see cref="IdentityUser.Email"/> is the private login;
/// <see cref="IdentityUser.UserName"/> is the public profile slug.
/// </summary>
public class AppUser : IdentityUser
{
    public const int DisplayNameMaxLength = 100;
    public const int NameMaxLength = 100;
    public const int BioMaxLength = 2000;
    public const int WebsiteUrlMaxLength = 2048;

    public required string DisplayName { get; set; }

    /// <summary>Private: shown only to the member themself.</summary>
    public string? FirstName { get; set; }

    /// <summary>Private: shown only to the member themself.</summary>
    public string? LastName { get; set; }

    /// <summary>Plain text (never HTML).</summary>
    public string? Bio { get; set; }

    /// <summary>Absolute http(s) URL.</summary>
    public string? WebsiteUrl { get; set; }

    public ProfileVisibility ProfileVisibility { get; set; } = ProfileVisibility.Public;

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Whether other people may see this member's profile.</summary>
    public bool IsPubliclyVisible => EmailConfirmed && ProfileVisibility == ProfileVisibility.Public;
}
