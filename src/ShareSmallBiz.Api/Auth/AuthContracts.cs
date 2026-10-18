using System.ComponentModel.DataAnnotations;
using ShareSmallBiz.Core.Users;

namespace ShareSmallBiz.Api.Auth;

// Validation attributes target the properties so they are enforced by AddValidation()
// and also published as constraints in the OpenAPI contract.

public sealed record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MinLength(10), MaxLength(128)] string Password,
    [property: Required, MinLength(UserNameRules.MinLength), MaxLength(UserNameRules.MaxLength), RegularExpression(UserNameRules.Pattern)] string UserName,
    [property: Required, MaxLength(100)] string DisplayName,
    [property: MaxLength(100)] string? FirstName = null,
    [property: MaxLength(100)] string? LastName = null);

public sealed record ConfirmEmailRequest(
    [property: Required] string UserId,
    [property: Required] string Token);

public sealed record EmailRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email);

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record RefreshRequest(
    [property: Required] string RefreshToken);

public sealed record ResetPasswordRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Token,
    [property: Required, MinLength(10), MaxLength(128)] string NewPassword);

public sealed record AuthTokensResponse(
    string TokenType,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

/// <summary>The signed-in user's own account. The only response that carries private fields.</summary>
public sealed record CurrentUserResponse(
    string Id,
    string Email,
    bool EmailConfirmed,
    string UserName,
    string DisplayName,
    string? FirstName,
    string? LastName,
    IReadOnlyList<string> Roles);
