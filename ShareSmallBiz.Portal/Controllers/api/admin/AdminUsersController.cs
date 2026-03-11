using Microsoft.AspNetCore.Authentication.JwtBearer;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers.api.admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AdminUsersController(
    ShareSmallBizUserContext context,
    ShareSmallBizUserManager userManager,
    RoleManager<IdentityRole> roleManager,
    ILogger<AdminUsersController> logger) : ControllerBase
{
    /// <summary>GET /api/admin/users — list all users with login history and roles</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? emailConfirmed,
        [FromQuery] string? role)
    {
        var users = await userManager.Users.OrderByDescending(u => u.LastModified).ToListAsync();

        var loginHistories = await context.LoginHistories
            .Where(lh => lh.Success)
            .GroupBy(lh => lh.UserId)
            .Select(g => new { UserId = g.Key, LastLogin = g.Max(lh => lh.LoginTime), LoginCount = g.Count() })
            .ToDictionaryAsync(x => x.UserId);

        var models = new List<UserModel>();
        foreach (var user in users)
        {
            var model = new UserModel(user)
            {
                Roles = new List<string>(await userManager.GetRolesAsync(user)),
                IsLockedOut = await userManager.IsLockedOutAsync(user)
            };

            if (loginHistories.TryGetValue(user.Id, out var history))
            {
                model.LastLogin = history.LastLogin;
                model.LoginCount = history.LoginCount;
            }

            models.Add(model);
        }

        if (emailConfirmed.HasValue)
            models = models.Where(u => u.IsEmailConfirmed == emailConfirmed.Value).ToList();

        if (!string.IsNullOrEmpty(role))
            models = models.Where(u => u.Roles.Contains(role)).ToList();

        return Ok(models);
    }

    /// <summary>GET /api/admin/users/{userId}</summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetById(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var model = new UserModel(user)
        {
            Roles = new List<string>(await userManager.GetRolesAsync(user)),
            IsLockedOut = await userManager.IsLockedOutAsync(user)
        };
        return Ok(model);
    }

    /// <summary>PUT /api/admin/users/{userId} — update user info</summary>
    [HttpPut("{userId}")]
    public async Task<IActionResult> Update(string userId, [FromBody] AdminUpdateUserRequest request)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        user.Email = request.Email ?? user.Email;
        user.NormalizedEmail = user.Email?.ToUpperInvariant();
        user.UserName = request.UserName ?? user.UserName;
        user.NormalizedUserName = user.UserName?.ToUpperInvariant();
        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.DisplayName = request.DisplayName ?? user.DisplayName;
        user.Bio = request.Bio ?? user.Bio;
        user.WebsiteUrl = request.WebsiteUrl ?? user.WebsiteUrl;
        user.LastModified = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        logger.LogInformation("Admin updated user {UserId}", userId);
        return NoContent();
    }

    /// <summary>DELETE /api/admin/users/{userId} — delete a user account</summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> Delete(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        logger.LogInformation("Admin deleted user {UserId}", userId);
        return NoContent();
    }

    /// <summary>POST /api/admin/users/{userId}/lock — toggle lock/unlock</summary>
    [HttpPost("{userId}/lock")]
    public async Task<IActionResult> ToggleLock(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var isLocked = await userManager.IsLockedOutAsync(user);
        var lockUntil = isLocked ? (DateTimeOffset?)null : DateTimeOffset.UtcNow.AddYears(100);
        await userManager.SetLockoutEndDateAsync(user, lockUntil);

        logger.LogInformation("Admin {Action} user {UserId}", isLocked ? "unlocked" : "locked", userId);
        return Ok(new { Locked = !isLocked });
    }

    /// <summary>GET /api/admin/users/{userId}/roles — list user's current roles</summary>
    [HttpGet("{userId}/roles")]
    public async Task<IActionResult> GetRoles(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        var allRoles = await roleManager.Roles.Select(r => r.Name).OrderBy(r => r).ToListAsync();
        return Ok(new { CurrentRoles = roles, AvailableRoles = allRoles });
    }

    /// <summary>PUT /api/admin/users/{userId}/roles — replace user's role assignments</summary>
    [HttpPut("{userId}/roles")]
    public async Task<IActionResult> SetRoles(string userId, [FromBody] SetUserRolesRequest request)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var current = await userManager.GetRolesAsync(user);
        var removeResult = await userManager.RemoveFromRolesAsync(user, current);
        if (!removeResult.Succeeded)
            return BadRequest(removeResult.Errors);

        if (request.Roles?.Count > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, request.Roles);
            if (!addResult.Succeeded)
                return BadRequest(addResult.Errors);
        }

        logger.LogInformation("Admin set roles for user {UserId}: {Roles}", userId, string.Join(", ", request.Roles ?? []));
        return NoContent();
    }

    /// <summary>POST /api/admin/users/business — create a pre-confirmed business user</summary>
    [HttpPost("business")]
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessUserRequest request)
    {
        var user = new ShareSmallBiz.Portal.Data.Entities.ShareSmallBizUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DisplayName = $"{request.FirstName} {request.LastName}",
            Slug = request.Email.ToLower().Replace("@", "-at-"),
            EmailConfirmed = true,
            Bio = request.Bio,
            WebsiteUrl = request.WebsiteUrl,
            LastModified = DateTime.UtcNow
        };

        var password = GenerateSecurePassword();
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await userManager.AddToRoleAsync(user, "Business");

        logger.LogInformation("Admin created business user {Email}", request.Email);
        return Ok(new { UserId = user.Id, Email = user.Email, TemporaryPassword = password });
    }

    private static string GenerateSecurePassword()
    {
        const string chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789!@$?_-";
        return new string(System.Security.Cryptography.RandomNumberGenerator
            .GetItems<char>(chars, 16));
    }
}

public class AdminUpdateUserRequest
{
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? WebsiteUrl { get; set; }
}

public class SetUserRolesRequest
{
    public List<string>? Roles { get; set; }
}

public class CreateBusinessUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? WebsiteUrl { get; set; }
}
