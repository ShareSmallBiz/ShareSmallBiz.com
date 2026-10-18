using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using ShareSmallBiz.Api.Auth;
using ShareSmallBiz.Api.Common;
using ShareSmallBiz.Core.Users;
using ShareSmallBiz.Infrastructure.Data;

namespace ShareSmallBiz.Api.Profiles;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var profiles = app.MapGroup("/api/v1/profiles").WithTags("Members: Profiles");

        profiles.MapGet("", ListProfiles)
            .WithName("ListProfiles")
            .WithSummary("Member directory")
            .WithDescription("""
                Public members, newest first. Only confirmed members with `Public` visibility are listed.

                `search` matches display name or username (case-insensitive, contains). `%` and `_` are matched literally.
                """)
            .ProducesValidationProblem();
        profiles.MapGet("/{userName}", GetProfile)
            .WithName("GetProfile")
            .WithSummary("A member's public profile")
            .WithDescription("""
                Looks up by username, case-insensitive. Returns **404** when the member does not exist, has not confirmed
                their email, or set their profile to `Private`; it never reveals which. Members always see their own profile.
                """);

        var me = app.MapGroup("/api/v1/me")
            .WithTags("Account: Me")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        me.MapGet("", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Your account")
            .WithDescription("Your own account, including private fields (email, real name, visibility). The only endpoint that returns them.");
        me.MapPut("/profile", UpdateProfile)
            .WithName("UpdateMyProfile")
            .WithSummary("Update your profile")
            .WithDescription("Replaces every editable field: omitted optional fields are cleared. Returns your updated account.")
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<Ok<PagedResponse<PublicProfileResponse>>> ListProfiles(
        AppDbContext db,
        CancellationToken cancellationToken,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, Paging.MaxPageSize)] int pageSize = Paging.DefaultPageSize,
        [MaxLength(100)] string? search = null)
    {
        var members = db.Users.AsNoTracking()
            .Where(u => u.EmailConfirmed && u.ProfileVisibility == ProfileVisibility.Public);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{EscapeLike(search.Trim())}%";
            members = members.Where(u =>
                EF.Functions.Like(u.DisplayName, pattern, "\\") || EF.Functions.Like(u.UserName!, pattern, "\\"));
        }

        var directory = members
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Select(u => new PublicProfileResponse(u.UserName!, u.DisplayName, u.Bio, u.WebsiteUrl, u.CreatedAt));

        return TypedResults.Ok(await directory.ToPagedResponseAsync(page, pageSize, cancellationToken));
    }

    private static async Task<Results<Ok<PublicProfileResponse>, NotFound>> GetProfile(
        string userName, ClaimsPrincipal principal, UserManager<AppUser> users)
    {
        var user = await users.FindByNameAsync(userName);
        var isSelf = user is not null && principal.FindFirstValue(JwtRegisteredClaimNames.Sub) == user.Id;
        if (user is null || !(user.IsPubliclyVisible || isSelf))
            return TypedResults.NotFound();

        return TypedResults.Ok(new PublicProfileResponse(user.UserName!, user.DisplayName, user.Bio, user.WebsiteUrl, user.CreatedAt));
    }

    private static async Task<Results<Ok<CurrentUserResponse>, UnauthorizedHttpResult>> GetCurrentUser(
        ClaimsPrincipal principal, UserManager<AppUser> users)
    {
        var user = await FindCurrentUserAsync(principal, users);
        if (user is null)
            return TypedResults.Unauthorized();
        return TypedResults.Ok(CurrentUserResponse.From(user, await users.GetRolesAsync(user)));
    }

    private static async Task<Results<Ok<CurrentUserResponse>, UnauthorizedHttpResult>> UpdateProfile(
        UpdateProfileRequest request, ClaimsPrincipal principal, UserManager<AppUser> users)
    {
        var user = await FindCurrentUserAsync(principal, users);
        if (user is null)
            return TypedResults.Unauthorized();

        user.DisplayName = request.DisplayName.Trim();
        user.FirstName = NullIfBlank(request.FirstName);
        user.LastName = NullIfBlank(request.LastName);
        user.Bio = NullIfBlank(request.Bio);
        user.WebsiteUrl = NullIfBlank(request.WebsiteUrl);
        user.ProfileVisibility = request.ProfileVisibility;

        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Profile update failed: {string.Join("; ", result.Errors.Select(e => e.Code))}");

        return TypedResults.Ok(CurrentUserResponse.From(user, await users.GetRolesAsync(user)));
    }

    private static async Task<AppUser?> FindCurrentUserAsync(ClaimsPrincipal principal, UserManager<AppUser> users) =>
        principal.FindFirstValue(JwtRegisteredClaimNames.Sub) is { } userId ? await users.FindByIdAsync(userId) : null;

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
