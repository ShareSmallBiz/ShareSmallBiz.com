using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ShareSmallBiz.Api.Common;
using ShareSmallBiz.Api.OpenApi;
using ShareSmallBiz.Core.Users;

namespace ShareSmallBiz.Api.Profiles;

// Directory query parameters are declared on the handler (see ProfileEndpoints.ListProfiles):
// [AsParameters] records with validation attributes crash build-time OpenAPI generation in .NET 10.

[Description("What anyone may see about a member. Never carries email, real names or account state.")]
public sealed record PublicProfileResponse(
    [property: Description("Profile slug, as used in `/api/v1/profiles/{userName}`.")] string UserName,
    string DisplayName,
    [property: Description("Plain text; render as text, never as HTML.")] string? Bio,
    string? WebsiteUrl,
    DateTimeOffset JoinedAt);

[Description("Replaces every editable field: omitted optional fields are cleared.")]
public sealed record UpdateProfileRequest(
    [property: Description("Public name shown on posts and the profile.")]
    [property: Required, MaxLength(AppUser.DisplayNameMaxLength), OpenApiExample("Corner Bakery")] string DisplayName,
    [property: Description("Private: visible only to you.")]
    [property: MaxLength(AppUser.NameMaxLength), OpenApiExample("Ada")] string? FirstName = null,
    [property: Description("Private: visible only to you.")]
    [property: MaxLength(AppUser.NameMaxLength), OpenApiExample("Lovelace")] string? LastName = null,
    [property: Description("Plain text, no HTML.")]
    [property: MaxLength(AppUser.BioMaxLength), OpenApiExample("Fresh sourdough every morning since 2009.")] string? Bio = null,
    [property: Description("Absolute `http` or `https` URL.")]
    [property: MaxLength(AppUser.WebsiteUrlMaxLength), HttpUrl, OpenApiExample("https://corner-bakery.example")] string? WebsiteUrl = null,
    [property: Description("`Private` hides the profile from everyone else (they get 404).")]
    ProfileVisibility ProfileVisibility = ProfileVisibility.Public);
