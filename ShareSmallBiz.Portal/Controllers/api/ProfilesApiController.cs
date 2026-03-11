using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Controllers.api;

[Route("api/profiles")]
public class ProfilesApiController(
    UserProvider userProvider,
    ILogger<ProfilesApiController> logger) : ApiControllerBase
{
    /// <summary>GET /api/profiles — list all public profiles</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var users = await userProvider.GetAllPublicUsersAsync();
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Populate follower/following counts and follow status
        foreach (var user in users)
        {
            user.FollowerCount = await userProvider.GetFollowerCountAsync(user.Id);
            user.FollowingCount = await userProvider.GetFollowingCountAsync(user.Id);

            if (!string.IsNullOrEmpty(currentUserId))
            {
                user.IsFollowedByMe = await userProvider.IsFollowedByMeAsync(currentUserId, user.Id);
            }
        }

        return Ok(users);
    }

    /// <summary>GET /api/profiles/{slug} — get single profile by username or custom URL slug</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var user = await userProvider.GetUserByUsernameAsync(slug);
        if (user is null)
        {
            logger.LogWarning("Profile not found for slug: {Slug}", slug);
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profileModel = new ProfileModel(user);

        // Populate follower/following counts
        profileModel.FollowerCount = await userProvider.GetFollowerCountAsync(user.Id);
        profileModel.FollowingCount = await userProvider.GetFollowingCountAsync(user.Id);

        // Set follow status (null for unauthenticated, bool for authenticated)
        if (!string.IsNullOrEmpty(currentUserId) && currentUserId != user.Id)
        {
            profileModel.IsFollowedByMe = await userProvider.IsFollowedByMeAsync(currentUserId, user.Id);
        }

        // Populate analytics for the profile owner
        if (currentUserId == user.Id)
        {
            profileModel.Analytics.TotalViews = user.ProfileViewCount;
            var followers = await userProvider.GetFollowersAsync(user.Id);
            profileModel.Analytics.Engagement.FollowerCount = followers.Count;
            profileModel.Analytics.Engagement.TotalLikes = user.LikeCount;
        }
        else
        {
            // Track view for non-owners
            await userProvider.IncrementProfileViewCountAsync(user.Id);
        }

        return Ok(profileModel);
    }

    /// <summary>GET /api/profiles/{slug}/followers</summary>
    [HttpGet("{slug}/followers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFollowers(string slug)
    {
        var user = await userProvider.GetUserByUsernameAsync(slug);
        if (user is null) return NotFound();

        var followers = await userProvider.GetFollowersAsync(user.Id);
        return Ok(followers);
    }

    /// <summary>GET /api/profiles/{slug}/following</summary>
    [HttpGet("{slug}/following")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFollowing(string slug)
    {
        var user = await userProvider.GetUserByUsernameAsync(slug);
        if (user is null) return NotFound();

        var following = await userProvider.GetFollowingAsync(user.Id);
        return Ok(following);
    }

    /// <summary>POST /api/profiles/{slug}/follow — follow a user (auth required)</summary>
    [HttpPost("{slug}/follow")]
    public async Task<IActionResult> Follow(string slug)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

        var target = await userProvider.GetUserByUsernameAsync(slug);
        if (target is null) return NotFound();
        if (target.Id == currentUserId) return BadRequest(new { Message = "Cannot follow yourself." });

        var result = await userProvider.FollowUserAsync(currentUserId, target.Id);
        return result ? NoContent() : BadRequest(new { Message = "Follow request failed." });
    }

    /// <summary>POST /api/profiles/{slug}/unfollow — unfollow a user (auth required)</summary>
    [HttpPost("{slug}/unfollow")]
    public async Task<IActionResult> Unfollow(string slug)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

        var target = await userProvider.GetUserByUsernameAsync(slug);
        if (target is null) return NotFound();

        var result = await userProvider.UnfollowUserAsync(currentUserId, target.Id);
        return result ? NoContent() : BadRequest(new { Message = "Unfollow request failed." });
    }
}
