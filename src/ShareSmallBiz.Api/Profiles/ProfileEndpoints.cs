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
        var profiles = app.MapGroup("/api/v1/profiles").WithTags("Profiles");

        profiles.MapGet("/", ListProfiles)
            .WithSummary("Directory of public members, newest first. Optional search on display name or username.");
        profiles.MapGet("/{userName}", GetProfile)
            .WithSummary("A member's public profile. 404 when the member does not exist or the profile is not public (unless it is your own).");

        app.MapGet("/api/v1/me", GetCurrentUser)
            .WithTags("Account")
            .WithSummary("The signed-in member's own account, including private fields.")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        app.MapPut("/api/v1/me/profile", UpdateProfile)
            .WithTags("Account")
            .WithSummary("Replace the signed-in member's editable profile fields.")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

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
