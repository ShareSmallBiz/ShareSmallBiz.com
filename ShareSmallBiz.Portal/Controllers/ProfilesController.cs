using ShareSmallBiz.Portal.Data.Enums;
using ShareSmallBiz.Portal.Infrastructure.Models;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Controllers;

[Route("Profiles")]
public class ProfilesController : Controller
{
    private readonly ILogger<ProfilesController> logger;
    private readonly UserProvider userProvider;

    public ProfilesController(ILogger<ProfilesController> logger, UserProvider userProvider)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.userProvider = userProvider ?? throw new ArgumentNullException(nameof(userProvider));
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var users = await userProvider.GetAllPublicUsersAsync();
        return View(users);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> ViewProfile(string id)
    {
        var users = await userProvider.GetAllPublicUsersAsync();
        var userModel = await userProvider.GetUserByUsernameAsync(id).ConfigureAwait(false);
        if (userModel == null)
        {
            logger.LogError("Missing Profile:{id}", id);
            return RedirectToAction("Index");
        }

        // Check if accessing via custom URL or attempt to use custom URL if available
        if (!string.IsNullOrEmpty(userModel.CustomProfileUrl))
        {
            if (!string.Equals(id, userModel.CustomProfileUrl, StringComparison.OrdinalIgnoreCase))
            {
                // Permanent redirect (301) to the custom URL if it exists
                return RedirectPermanent($"/Profiles/{userModel.CustomProfileUrl}");
            }
        }
        // If no custom URL but the ID doesn't match DisplayName, redirect to canonical URL
        else if (!string.Equals(id, userModel.DisplayName, StringComparison.Ordinal))
        {
            // Permanent redirect (301) to the correct userModel URL
            return RedirectPermanent($"/Profiles/{userModel.DisplayName}");
        }
        
        // Check visibility permissions
        if (!HasProfileAccessPermission(userModel))
        {
            return RedirectToAction("AccessDenied", "Error", new { message = "This profile is not publicly accessible" });
        }
        
        // Track profile view if the viewer is not the profile owner
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId != userModel.Id)
        {
            await TrackProfileView(userModel.Id);
        }
        
        var profileModel = new ProfileModel(userModel);
        profileModel.PublicUsers = users;
        
        // Fetch and populate analytics data if the viewer is the profile owner
        if (currentUserId == userModel.Id)
        {
            await PopulateAnalyticsData(profileModel);
        }
        
        return View(profileModel);
    }
    
    /// <summary>
    /// Checks if the current user has permission to view the specified profile
    /// </summary>
    private bool HasProfileAccessPermission(UserModel profile)
    {
        // Allow public profiles for everyone
        if (profile.ProfileVisibility == ProfileVisibility.Public)
        {
            return true;
        }
        
        // Profile owner always has access
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == profile.Id)
        {
            return true;
        }
        
        // For authenticated-only profiles, check if user is logged in
        if (profile.ProfileVisibility == ProfileVisibility.Authenticated)
        {
            return User.Identity?.IsAuthenticated ?? false;
        }
        
        // For connection-only profiles, check if the user follows or is followed by the profile owner
        if (profile.ProfileVisibility == ProfileVisibility.Connections)
        {
            if (string.IsNullOrEmpty(currentUserId))
            {
                return false;
            }
            
            // Check connection status asynchronously (converted to sync call for this method)
            var task = IsConnectionAsync(currentUserId, profile.Id);
            task.Wait();
            return task.Result;
        }
        
        // Private profiles are only accessible to the owner (already checked above)
        return false;
    }
    
    /// <summary>
    /// Checks if two users follow each other
    /// </summary>
    private async Task<bool> IsConnectionAsync(string userId1, string userId2)
    {
        // Check if either user follows the other
        var follows1 = await userProvider.CheckFollowingAsync(userId1, userId2);
        var follows2 = await userProvider.CheckFollowingAsync(userId2, userId1);
        
        return follows1 || follows2;
    }
    
    /// <summary>
    /// Tracks a profile view for analytics
    /// </summary>
    private async Task TrackProfileView(string profileId)
    {
        await userProvider.IncrementProfileViewCountAsync(profileId);
        
        // Additional analytics tracking could be implemented here
        // e.g., storing geolocation data, referrer info, etc.
    }
    
    /// <summary>
    /// Populates analytics data for profile owner view
    /// </summary>
    private async Task PopulateAnalyticsData(ProfileModel profile)
    {
        // Set total view count
        profile.Analytics.TotalViews = profile.ProfileViewCount;
        
        // Get follower metrics
        var followers = await userProvider.GetFollowersAsync(profile.Id);
        profile.Analytics.Engagement.FollowerCount = followers.Count;
        
        // Calculate new followers in last 30 days (placeholder implementation)
        profile.Analytics.Engagement.NewFollowers = 0; // This would need tracking of follow dates
        
        // Set like metrics
        profile.Analytics.Engagement.TotalLikes = profile.LikeCount;
        
        // Recent likes would need implementation with tracking of like dates
        profile.Analytics.Engagement.RecentLikes = 0;
        
        // Populate recent views data (placeholder)
        profile.Analytics.RecentViews = new Dictionary<DateTime, int>
        {
            { DateTime.Today, profile.ProfileViewCount } // This would need actual tracking data
        };
    }
}
