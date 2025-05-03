using Microsoft.Extensions.Options;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Data.Enums;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class ShareSmallBizUserManager(
    IUserStore<ShareSmallBizUser> store,
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<ShareSmallBizUser> passwordHasher,
    IEnumerable<IUserValidator<ShareSmallBizUser>> userValidators,
    IEnumerable<IPasswordValidator<ShareSmallBizUser>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<ShareSmallBizUserManager> logger,
    ShareSmallBizUserContext context,
    RoleManager<IdentityRole> roleManager
    ) : UserManager<ShareSmallBizUser>(store, optionsAccessor, passwordHasher, userValidators,
         passwordValidators, keyNormalizer, errors, services, logger)
{
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;

    public async Task<ShareSmallBizUser?> GetFullUserAsync(ClaimsPrincipal principal)
    {
        var id = GetUserId(principal);
        if (id == null)
        {
            return null;
        }

        var user = await Users
            .Include(u => u.SocialLinks)
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id)
            .ConfigureAwait(false);

        return user;
    }

    /// <summary>
    /// Retrieves the social links of a user by their ID.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of SocialLink objects.</returns>
    public async Task<List<SocialLink>> GetUserSocialLinksAsync(string userId, CancellationToken ct = default)
    {
        var socialLinks = await context.SocialLinks
            .Where(sl => sl.UserId == userId)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Retrieved {Count} social links for user {UserId}.",
                              socialLinks.Count, userId);
        return socialLinks ?? [];
    }

    /// <summary>
    /// Gets all roles for a user efficiently using the navigation property
    /// </summary>
    public async Task<IList<string>> GetUserRolesEfficientAsync(ShareSmallBizUser user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        // If UserRoles are already loaded, use them directly
        if (user.UserRoles != null && user.UserRoles.Any())
        {
            return user.UserRoles
                .Select(ur => ur.RoleId ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
        }

        // Otherwise, load them from the database
        var roles = await context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(
                context.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => role.Name
            )
            .ToListAsync();

        return roles!;
    }

    /// <summary>
    /// Checks if a user is in a specific role efficiently
    /// </summary>
    public async Task<bool> IsInRoleEfficientAsync(ShareSmallBizUser user, string roleName)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name cannot be empty", nameof(roleName));
        }

        // If UserRoles are already loaded, use them directly
        if (user.UserRoles != null && user.UserRoles.Any())
        {
            return user.UserRoles.Any(ur =>
                string.Equals(ur.RoleId, roleName, StringComparison.OrdinalIgnoreCase));
        }

        // Get the role ID first for more efficient query
        var roleId = await _roleManager.Roles
            .Where(r => r.NormalizedName == roleName.ToUpper())
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(roleId))
        {
            return false;
        }

        // Check if the user has this role
        return await context.UserRoles
            .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == roleId);
    }
}

public class UserProvider(
    ShareSmallBizUserContext context,
    ShareSmallBizUserManager userManager,
    RoleManager<IdentityRole> roleManager,
    ILogger<UserProvider> logger
)
{
    // Private method to map from EF entity to UserModel
    private static UserModel MapToUserModel(ShareSmallBizUser user)
    {
        return new UserModel(user);
    }

    private static ProfileModel MapToProfileModel(ShareSmallBizUser user)
    {
        return new ProfileModel(user);
    }

    // Cache the business role ID to avoid repeated lookups
    private string? _businessRoleId;
    private async Task<string?> GetBusinessRoleIdAsync()
    {
        if (_businessRoleId == null)
        {
            var businessRole = await roleManager.Roles
                .Where(r => r.Name == "Business")
                .FirstOrDefaultAsync();

            _businessRoleId = businessRole?.Id;
        }

        return _businessRoleId;
    }

    // Create a new user
    public async Task<bool> CreateUserAsync(UserModel model, string password)
    {
        var user = new ShareSmallBizUser
        {
            UserName = model.UserName,
            Email = model.Email,
            DisplayName = model.DisplayName,
            Bio = model.Bio,
            FirstName = model.FirstName,
            LastName = model.LastName,
            ProfilePictureUrl = model.ProfilePictureUrl,
            Slug = model.UserName // Ensure slug is set to improve discoverability
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            logger.LogInformation("User {UserName} created successfully.", user.UserName);
        }
        else
        {
            logger.LogError("Failed to create user {UserName}. Errors: {Errors}",
                user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        return result.Succeeded;
    }

    // Delete user
    public async Task<bool> DeleteUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found.", userId);
            return false;
        }

        var result = await userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            logger.LogInformation("User {UserName} deleted successfully.", user.UserName);
        }
        else
        {
            logger.LogError("Failed to delete user {UserName}. Errors: {Errors}",
                user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        return result.Succeeded;
    }

    // Follow another user
    public async Task<bool> FollowUserAsync(string followerId, string followingId)
    {
        if (followerId == followingId)
        {
            logger.LogWarning("User {UserId} cannot follow themselves.", followerId);
            return false;
        }

        // Check if the follow relationship already exists
        bool alreadyFollowing = await context.UserFollows
            .AnyAsync(uf => uf.FollowerId == followerId && uf.FollowingId == followingId);

        if (!alreadyFollowing)
        {
            context.UserFollows.Add(new UserFollow { FollowerId = followerId, FollowingId = followingId });
            await context.SaveChangesAsync();
            logger.LogInformation("User {FollowerId} followed user {FollowingId}.", followerId, followingId);
            return true;
        }

        logger.LogWarning("User {FollowerId} is already following user {FollowingId}.", followerId, followingId);
        return false;
    }

    // Retrieve all business users
    public async Task<List<UserModel>> GetAllPublicUsersAsync()
    {
        var businessRoleId = await GetBusinessRoleIdAsync();
        if (string.IsNullOrEmpty(businessRoleId))
        {
            logger.LogWarning("Business role not found.");
            return [];
        }

        // Efficiently query users in the business role using navigation properties
        var usersInBusinessRole = await context.Users
            .Where(u => u.Email != u.DisplayName) // Filter non-public users
            .Where(u => u.UserRoles.Any(ur => ur.RoleId == businessRoleId))
            .Include(u => u.Posts)
            .AsNoTracking()
            .ToListAsync();

        // Fix usernames if necessary
        var usersToUpdate = usersInBusinessRole
            .Where(u => u.UserName != u.DisplayName && u.UserName == u.Email)
            .ToList();

        if (usersToUpdate.Any())
        {
            foreach (var user in usersToUpdate)
            {
                var updateUser = await context.Users
                    .SingleOrDefaultAsync(s => s.Id == user.Id);

                if (updateUser != null)
                {
                    logger.LogWarning("User {UserName} has a username that is the same as their email address. Updating username to display name.", user.UserName);
                    updateUser.UserName = user.DisplayName;
                    updateUser.Slug = user.DisplayName;
                    await userManager.UpdateAsync(updateUser);
                }
            }
        }

        logger.LogInformation("Retrieved {UserCount} business users.", usersInBusinessRole.Count);
        return usersInBusinessRole.Select(MapToUserModel).ToList();
    }

    // Get followers of a user
    public async Task<List<UserModel>> GetFollowersAsync(string userId)
    {
        var followers = await context.UserFollows
            .Where(uf => uf.FollowingId == userId)
            .Select(uf => uf.Follower)
            .AsNoTracking()
            .ToListAsync();

        logger.LogInformation("User {UserId} has {FollowerCount} followers.", userId, followers.Count);
        return followers.Select(MapToUserModel).ToList();
    }

    // Get users a user is following
    public async Task<List<UserModel>> GetFollowingAsync(string userId)
    {
        var following = await context.UserFollows
            .Where(uf => uf.FollowerId == userId)
            .Select(uf => uf.Following)
            .AsNoTracking()
            .ToListAsync();

        logger.LogInformation("User {UserId} is following {FollowingCount} users.", userId, following.Count);
        return following.Select(MapToUserModel).ToList();
    }

    // Retrieve a user by ID with roles
    public async Task<UserModel?> GetUserByIdAsync(string userId)
    {
        var user = await context.Users
            .Include(u => u.UserRoles)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found.", userId);
            return null;
        }

        var userModel = MapToUserModel(user);

        // Add roles information from the navigation property
        if (user.UserRoles != null)
        {
            userModel.Roles = user.UserRoles
                .Select(ur => ur.RoleId)
                .Where(name => name != null)
                .ToList()!;
        }

        return userModel;
    }

    // Get profile by username with efficient role lookup
    public async Task<ProfileModel?> GetProfileByUsernameAsync(string username)
    {
        // Try to find by exact username match first
        var user = await context.Users
            .Include(u => u.Posts)
            .Include(u => u.LikedPosts)
            .Include(u => u.UserRoles)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == username);

        // If not found, try normalized search
        if (user == null)
        {
            var normalizedInput = username.Replace(" ", string.Empty).ToLowerInvariant();

            user = await context.Users
                .Include(u => u.Posts)
                .Include(u => u.LikedPosts)
                .Include(u => u.UserRoles)
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.UserName.Replace(" ", string.Empty).ToLower() == normalizedInput);

            if (user == null)
            {
                logger.LogWarning("User with username {Username} not found.", username);
                return null;
            }
        }

        var profileModel = MapToProfileModel(user);

        // Add roles from navigation property
        if (user.UserRoles != null)
        {
            profileModel.Roles = user.UserRoles
                .Select(ur => ur.RoleId)
                .Where(name => name != null)
                .ToList()!;
        }

        return profileModel;
    }

    // Get user by username with roles
    public async Task<UserModel?> GetUserByUsernameAsync(string username)
    {
        // Try to find by display name first
        var user = await context.Users
            .Include(u => u.Posts)
            .Include(u => u.LikedPosts)
            .Include(u => u.UserRoles)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.DisplayName == username);

        // If not found, try normalized search
        if (user == null)
        {
            var normalizedInput = username.Replace(" ", string.Empty).ToLowerInvariant();

            user = await context.Users
                .Include(u => u.Posts)
                .Include(u => u.LikedPosts)
                .Include(u => u.UserRoles)
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.UserName.Replace(" ", string.Empty).ToLower() == normalizedInput);

            if (user == null)
            {
                logger.LogWarning("User with username {Username} not found.", username);
                return null;
            }
        }

        var userModel = MapToUserModel(user);

        return userModel;
    }
    public async Task<bool> UpdateUserAsync(string userId, UserModel model)
    {
        // Validate input parameters
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("Invalid userId: null or empty value provided");
            return false;
        }

        if (model == null)
        {
            logger.LogWarning("Invalid model: null value provided for userId {UserId}", userId);
            return false;
        }

        // Try to find user directly in context first to avoid additional query from UserManager
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found", userId);
            return false;
        }

        // Track changes to include in log message
        var changedProperties = new List<string>();

        // Only update properties that have actually changed
        if (user.DisplayName != model.DisplayName)
        {
            user.DisplayName = model.DisplayName;
            changedProperties.Add("DisplayName");

            // Update slug when display name changes
            if (!string.IsNullOrEmpty(model.DisplayName))
            {
                user.Slug = model.DisplayName.ToLowerInvariant()
                    .Replace(" ", "-")
                    .Replace("'", string.Empty)
                    .Replace("\"", string.Empty);
                changedProperties.Add("Slug");
            }
        }

        if (user.Bio != model.Bio)
        {
            user.Bio = model.Bio;
            changedProperties.Add("Bio");
        }

        if (user.FirstName != model.FirstName)
        {
            user.FirstName = model.FirstName;
            changedProperties.Add("FirstName");
        }

        if (user.LastName != model.LastName)
        {
            user.LastName = model.LastName;
            changedProperties.Add("LastName");
        }

        if (user.ProfilePictureUrl != model.ProfilePictureUrl)
        {
            user.ProfilePictureUrl = model.ProfilePictureUrl;
            changedProperties.Add("ProfilePictureUrl");
        }

        // Only update if something actually changed
        if (changedProperties.Count == 0)
        {
            logger.LogInformation("No changes detected for user {UserName} ({UserId})", user.UserName, userId);
            return true; // Return true as the request was successful, even though no changes were made
        }

        user.LastModified = DateTime.UtcNow;
        changedProperties.Add("LastModified");

        // Use UserManager for the update to ensure proper validation and events are triggered
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            logger.LogInformation("User {UserName} ({UserId}) updated successfully. Changed properties: {Properties}",
                user.UserName, userId, string.Join(", ", changedProperties));
            return true;
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to update user {UserName} ({UserId}). Errors: {Errors}",
                user.UserName, userId, errors);
            return false;
        }
    }

    public async Task<bool> UnfollowUserAsync(string followerId, string followingId)
    {
        // Input validation
        if (string.IsNullOrEmpty(followerId) || string.IsNullOrEmpty(followingId))
        {
            logger.LogWarning("Invalid parameters: follower or following ID is null or empty");
            return false;
        }

        if (followerId == followingId)
        {
            logger.LogWarning("Invalid operation: User {UserId} cannot unfollow themselves", followerId);
            return false;
        }

        try
        {
            // Use ExecuteDeleteAsync for better performance when supported (.NET 7+)
            int deletedCount = await context.UserFollows
                .Where(uf => uf.FollowerId == followerId && uf.FollowingId == followingId)
                .ExecuteDeleteAsync();

            if (deletedCount > 0)
            {
                logger.LogInformation("User {FollowerId} unfollowed user {FollowingId}", followerId, followingId);
                return true;
            }

            logger.LogInformation("User {FollowerId} is not following user {FollowingId}", followerId, followingId);
            return false;
        }
        catch (Exception ex)
        {
            // Fallback for older EF Core versions or if ExecuteDeleteAsync fails
            if (ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Error unfollowing user {FollowingId} by {FollowerId}", followingId, followerId);
                throw;
            }

            // Traditional approach as fallback
            var follow = await context.UserFollows
                .Where(uf => uf.FollowerId == followerId && uf.FollowingId == followingId)
                .FirstOrDefaultAsync();

            if (follow != null)
            {
                context.UserFollows.Remove(follow);
                await context.SaveChangesAsync();
                logger.LogInformation("User {FollowerId} unfollowed user {FollowingId}", followerId, followingId);
                return true;
            }

            logger.LogInformation("User {FollowerId} is not following user {FollowingId}", followerId, followingId);
            return false;
        }
    }

    /// <summary>
    /// Checks if a user is following another user
    /// </summary>
    /// <param name="followerId">The ID of the follower user</param>
    /// <param name="followingId">The ID of the user being followed</param>
    /// <returns>True if the follower is following the other user</returns>
    public async Task<bool> CheckFollowingAsync(string followerId, string followingId)
    {
        if (string.IsNullOrEmpty(followerId) || string.IsNullOrEmpty(followingId))
        {
            return false;
        }

        return await context.UserFollows
            .AnyAsync(uf => uf.FollowerId == followerId && uf.FollowingId == followingId);
    }

    /// <summary>
    /// Increments the profile view count for a user
    /// </summary>
    /// <param name="userId">The ID of the user whose profile was viewed</param>
    /// <returns>True if the operation was successful</returns>
    public async Task<bool> IncrementProfileViewCountAsync(string userId)
    {
        try
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null)
            {
                logger.LogWarning("User with ID {UserId} not found when trying to increment profile view count", userId);
                return false;
            }

            user.ProfileViewCount++;
            user.LastModified = DateTime.UtcNow;
            
            // Store view for analytics (optional implementation)
            // context.ProfileViews.Add(new ProfileView 
            // { 
            //     UserId = userId, 
            //     ViewDate = DateTime.UtcNow,
            //     IPAddress = GetCurrentIP(), // Would need implementation
            //     UserAgent = GetCurrentUserAgent(), // Would need implementation
            //     Location = GetCurrentLocation() // Would need implementation
            // });
            
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error incrementing profile view count for user {UserId}", userId);
            return false;
        }
    }
    
    /// <summary>
    /// Updates the custom profile URL for a user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="customUrl">The new custom profile URL</param>
    /// <returns>True if successful, false if the URL is already taken or invalid</returns>
    public async Task<(bool Success, string? ErrorMessage)> UpdateCustomProfileUrlAsync(string userId, string customUrl)
    {
        if (string.IsNullOrWhiteSpace(customUrl))
        {
            return (false, "Custom URL cannot be empty");
        }
        
        // Validate custom URL format (alphanumeric, hyphens, and underscores only)
        if (!Regex.IsMatch(customUrl, @"^[a-zA-Z0-9\-_]+$"))
        {
            return (false, "Custom URL can only contain letters, numbers, hyphens, and underscores");
        }
        
        // Check if URL is already taken by another user
        bool isUrlTaken = await context.Users
            .AnyAsync(u => u.CustomProfileUrl == customUrl && u.Id != userId);
            
        if (isUrlTaken)
        {
            return (false, "This custom URL is already taken by another user");
        }
        
        try
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }
            
            user.CustomProfileUrl = customUrl;
            user.LastModified = DateTime.UtcNow;
            await context.SaveChangesAsync();
            
            logger.LogInformation("Updated custom profile URL for user {UserId} to {CustomUrl}", userId, customUrl);
            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating custom profile URL for user {UserId}", userId);
            return (false, "An error occurred while updating the custom profile URL");
        }
    }
    
    /// <summary>
    /// Updates the profile visibility setting for a user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="visibility">The new visibility setting</param>
    /// <returns>True if successful</returns>
    public async Task<bool> UpdateProfileVisibilityAsync(string userId, ProfileVisibility visibility)
    {
        try
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null)
            {
                logger.LogWarning("User with ID {UserId} not found when trying to update profile visibility", userId);
                return false;
            }
            
            user.ProfileVisibility = visibility;
            user.LastModified = DateTime.UtcNow;
            await context.SaveChangesAsync();
            
            logger.LogInformation("Updated profile visibility for user {UserId} to {Visibility}", userId, visibility);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating profile visibility for user {UserId}", userId);
            return false;
        }
    }
    
    /// <summary>
    /// Calculates and updates the profile completeness score for a user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <returns>The updated completeness score (0-100)</returns>
    public async Task<int> UpdateProfileCompletenessScoreAsync(string userId)
    {
        try
        {
            var user = await context.Users
                .Include(u => u.SocialLinks)
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user == null)
            {
                logger.LogWarning("User with ID {UserId} not found when trying to calculate profile completeness", userId);
                return 0;
            }
            
            int score = 0;
            int totalPossiblePoints = 100;
            int fieldCount = 8; // Total number of profile fields we're checking
            int pointsPerField = totalPossiblePoints / fieldCount;
            
            // Basic profile fields (50%)
            if (!string.IsNullOrWhiteSpace(user.DisplayName)) score += pointsPerField;
            if (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName)) score += pointsPerField;
            if (!string.IsNullOrWhiteSpace(user.Bio)) score += pointsPerField;
            if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl)) score += pointsPerField;
            
            // SEO & contact fields (25%)
            if (!string.IsNullOrWhiteSpace(user.MetaDescription)) score += pointsPerField;
            if (!string.IsNullOrWhiteSpace(user.WebsiteUrl)) score += pointsPerField;
            
            // Social connections (25%)
            if (user.SocialLinks != null && user.SocialLinks.Any()) score += pointsPerField;
            if (await context.UserFollows.AnyAsync(uf => uf.FollowerId == userId)) score += pointsPerField;
            
            // Ensure score is within 0-100 range
            score = Math.Min(100, Math.Max(0, score));
            
            // Update the user's profile completeness score
            user.ProfileCompletenessScore = score;
            user.LastModified = DateTime.UtcNow;
            await context.SaveChangesAsync();
            
            logger.LogInformation("Updated profile completeness score for user {UserId} to {Score}", userId, score);
            return score;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating profile completeness score for user {UserId}", userId);
            return 0;
        }
    }
}
