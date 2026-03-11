using Microsoft.AspNetCore.Identity;
using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Data.Enums;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Controllers.api;

/// <summary>
/// User settings controller for managing notification and privacy preferences.
/// </summary>
[Route("api/users/{userId}/settings")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class UserSettingsController(
    UserProvider userProvider,
    ILogger<UserSettingsController> logger,
    UserManager<ShareSmallBizUser> userManager) : ControllerBase
{
    /// <summary>
    /// Gets user notification and privacy settings.
    /// </summary>
    /// <param name="userId">The user ID (must match authenticated user)</param>
    /// <returns>User settings model with notifications and privacy preferences</returns>
    [HttpGet]
    public async Task<IActionResult> GetSettings(string userId)
    {
        // Authorization: Verify caller is account owner
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId != userId)
            return Forbid();

        try
        {
            var settings = await userProvider.GetUserSettingsAsync(userId);
            if (settings == null)
                return NotFound(new { message = "User not found." });

            return Ok(settings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving settings for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error retrieving user settings." });
        }
    }

    /// <summary>
    /// Updates user notification and privacy settings.
    /// </summary>
    /// <param name="userId">The user ID (must match authenticated user)</param>
    /// <param name="request">Settings update request</param>
    /// <returns>Success response</returns>
    [HttpPut]
    public async Task<IActionResult> UpdateSettings(string userId, [FromBody] UpdateUserSettingRequest request)
    {
        // Authorization: Verify caller is account owner
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId != userId)
            return Forbid();

        // Validate request
        if (request == null)
            return BadRequest(new { message = "Request body is required." });

        try
        {
            var success = await userProvider.UpdateUserSettingsAsync(userId, request);
            if (!success)
                return NotFound(new { message = "User not found." });

            return Ok(new { message = "Settings updated successfully." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating settings for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error updating user settings." });
        }
    }
}

/// <summary>
/// User settings model containing notification and privacy preferences.
/// </summary>
public class UserSettingModel
{
    /// <summary>
    /// Notification preferences settings
    /// </summary>
    public class NotificationSettings
    {
        /// <summary>
        /// Send email when someone comments on your post
        /// </summary>
        public bool EmailOnComment { get; set; } = true;

        /// <summary>
        /// Send email when someone likes your post
        /// </summary>
        public bool EmailOnLike { get; set; } = false;

        /// <summary>
        /// Send email when someone follows you
        /// </summary>
        public bool EmailOnFollow { get; set; } = true;

        /// <summary>
        /// Send weekly summary email
        /// </summary>
        public bool WeeklySummary { get; set; } = false;
    }

    /// <summary>
    /// Privacy preferences settings
    /// </summary>
    public class PrivacySettings
    {
        /// <summary>
        /// Profile visibility: Public, Authenticated, Connections, Private
        /// </summary>
        public ProfileVisibility ProfileVisibility { get; set; } = ProfileVisibility.Public;

        /// <summary>
        /// Show email address on profile
        /// </summary>
        public bool ShowEmail { get; set; } = false;

        /// <summary>
        /// Show website URL on profile
        /// </summary>
        public bool ShowWebsite { get; set; } = true;
    }

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Notification preferences
    /// </summary>
    public NotificationSettings Notifications { get; set; } = new();

    /// <summary>
    /// Privacy preferences
    /// </summary>
    public PrivacySettings Privacy { get; set; } = new();
}

/// <summary>
/// Request model for updating user settings
/// </summary>
public class UpdateUserSettingRequest
{
    /// <summary>
    /// Notification preferences
    /// </summary>
    public UserSettingModel.NotificationSettings Notifications { get; set; } = new();

    /// <summary>
    /// Privacy preferences
    /// </summary>
    public UserSettingModel.PrivacySettings Privacy { get; set; } = new();
}
