using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ShareSmallBiz.Api.OpenApi;
using ShareSmallBiz.Core.Users;

namespace ShareSmallBiz.Api.Auth;

// Validation attributes target the properties so they are enforced by AddValidation()
// and also published as constraints in the OpenAPI contract. Examples are chosen to work together:
// the register example's email and password are the login example's, so pre-filled bodies run in sequence.

internal static class AuthExamples
{
    public const string Email = "owner@corner-bakery.example";
    public const string Password = "correct horse battery";
}

[Description("New member sign-up.")]
public sealed record RegisterRequest(
    [property: Description("Private sign-in email; never shown publicly.")]
    [property: Required, EmailAddress, MaxLength(256), OpenApiExample(AuthExamples.Email)] string Email,
    [property: Description("At least 10 characters; a passphrase is ideal.")]
    [property: Required, MinLength(10), MaxLength(128), OpenApiExample(AuthExamples.Password)] string Password,
    [property: Description("Public profile slug used in URLs: lowercase letters, digits and single hyphens.")]
    [property: Required, MinLength(UserNameRules.MinLength), MaxLength(UserNameRules.MaxLength), RegularExpression(UserNameRules.Pattern), OpenApiExample("corner-bakery")] string UserName,
    [property: Description("Public name shown on posts and the profile.")]
    [property: Required, MaxLength(100), OpenApiExample("Corner Bakery")] string DisplayName,
    [property: Description("Private.")]
    [property: MaxLength(100), OpenApiExample("Ada")] string? FirstName = null,
    [property: Description("Private.")]
    [property: MaxLength(100), OpenApiExample("Lovelace")] string? LastName = null);

[Description("Values from the confirmation link (`/confirm-email?userId=…&token=…`).")]
public sealed record ConfirmEmailRequest(
    [property: Description("The `userId` query value from the confirmation link.")]
    [property: Required, OpenApiExample("paste userId from the confirmation link")] string UserId,
    [property: Description("The `token` query value from the confirmation link, as-is.")]
    [property: Required, OpenApiExample("paste token from the confirmation link")] string Token);

public sealed record EmailRequest(
    [property: Required, EmailAddress, MaxLength(256), OpenApiExample(AuthExamples.Email)] string Email);

public sealed record LoginRequest(
    [property: Required, EmailAddress, OpenApiExample(AuthExamples.Email)] string Email,
    [property: Required, OpenApiExample(AuthExamples.Password)] string Password);

public sealed record RefreshRequest(
    [property: Description("The `refreshToken` from login or the previous refresh. Works once.")]
    [property: Required, OpenApiExample("paste refreshToken from login")] string RefreshToken);

[Description("Values from the reset link (`/reset-password?email=…&token=…`) plus the new password.")]
public sealed record ResetPasswordRequest(
    [property: Required, EmailAddress, OpenApiExample(AuthExamples.Email)] string Email,
    [property: Description("The `token` query value from the reset link, as-is.")]
    [property: Required, OpenApiExample("paste token from the reset link")] string Token,
    [property: Required, MinLength(10), MaxLength(128), OpenApiExample("a brand new passphrase")] string NewPassword);

[Description("Issued by login and refresh.")]
public sealed record AuthTokensResponse(
    [property: Description("Always `Bearer`.")] string TokenType,
    [property: Description("Send as `Authorization: Bearer <accessToken>`.")] string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    [property: Description("Exchange at `POST /api/v1/auth/refresh` before it expires. Single use.")] string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

/// <summary>The signed-in member's own account. The only response that carries private fields.</summary>
[Description("Your own account, including private fields. Only ever returned to you.")]
public sealed record CurrentUserResponse(
    string Id,
    string Email,
    bool EmailConfirmed,
    string UserName,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Bio,
    string? WebsiteUrl,
    ProfileVisibility ProfileVisibility,
    DateTimeOffset JoinedAt,
    IReadOnlyList<string> Roles)
{
    public static CurrentUserResponse From(AppUser user, IEnumerable<string> roles) => new(
        user.Id, user.Email!, user.EmailConfirmed, user.UserName!, user.DisplayName, user.FirstName, user.LastName,
        user.Bio, user.WebsiteUrl, user.ProfileVisibility, user.CreatedAt, [.. roles]);
}
