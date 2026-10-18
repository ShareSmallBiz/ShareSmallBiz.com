using Microsoft.AspNetCore.Identity;

namespace ShareSmallBiz.Core.Users;

/// <summary>
/// A member account. <see cref="IdentityUser.Email"/> is the private login;
/// <see cref="IdentityUser.UserName"/> is the public profile slug.
/// </summary>
public class AppUser : IdentityUser
{
    public required string DisplayName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
