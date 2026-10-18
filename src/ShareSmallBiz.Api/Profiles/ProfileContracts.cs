using System.ComponentModel.DataAnnotations;
using ShareSmallBiz.Api.Common;
using ShareSmallBiz.Core.Users;

// Directory query parameters are declared on the handler (see ProfileEndpoints.ListProfiles):
// [AsParameters] records with validation attributes crash build-time OpenAPI generation in .NET 10.

namespace ShareSmallBiz.Api.Profiles;

/// <summary>What anyone may see about a member. Never carries email, real names or account state.</summary>
public sealed record PublicProfileResponse(
    string UserName,
    string DisplayName,
    string? Bio,
    string? WebsiteUrl,
    DateTimeOffset JoinedAt);

/// <summary>Replaces the member's editable profile fields.</summary>
public sealed record UpdateProfileRequest(
    [property: Required, MaxLength(AppUser.DisplayNameMaxLength)] string DisplayName,
    [property: MaxLength(AppUser.NameMaxLength)] string? FirstName = null,
    [property: MaxLength(AppUser.NameMaxLength)] string? LastName = null,
    [property: MaxLength(AppUser.BioMaxLength)] string? Bio = null,
    [property: MaxLength(AppUser.WebsiteUrlMaxLength), HttpUrl] string? WebsiteUrl = null,
    ProfileVisibility ProfileVisibility = ProfileVisibility.Public);
