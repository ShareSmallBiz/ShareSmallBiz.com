using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class UserProvider(
    ShareSmallBizUserContext context,
    UserManager<ShareSmallBizUser> userManager,
    ILogger<UserProvider> logger
    )
{

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
            ProfilePictureUrl = model.ProfilePictureUrl
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            logger.LogInformation("User {UserName} created successfully.", user.UserName);
        }
        else
        {
            logger.LogError("Failed to create user {UserName}. Errors: {Errors}", user.UserName, result.Errors);
        }
        return result.Succeeded;
    }

    // Retrieve a user by ID
    public async Task<UserModel?> GetUserByIdAsync(string userId)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found.", userId);
            return null;
        }

        return MapToUserModel(user);
    }

    public async Task<UserModel?> GetUserByUsernameAsync(string username)
    {
        var user = await context.Users.Include(u => u.Posts).Include(u => u.LikedPosts)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == username);

        if (user == null)
        {
            logger.LogWarning("User with username {Username} not found.", username);
            return null;
        }

        return MapToUserModel(user);
    }



    // Retrieve all users
    public async Task<List<UserModel>> GetAllUsersAsync()
    {
        var users = await context.Users.AsNoTracking().ToListAsync();
        logger.LogInformation("Retrieved {UserCount} users.", users.Count);
        return users.Select(MapToUserModel).ToList();
    }

    // Update user details
    public async Task<bool> UpdateUserAsync(string userId, UserModel model)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found.", userId);
            return false;
        }

        user.DisplayName = model.DisplayName;
        user.Bio = model.Bio;
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.ProfilePictureUrl = model.ProfilePictureUrl;

        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            logger.LogInformation("User {UserName} updated successfully.", user.UserName);
        }
        else
        {
            logger.LogError("Failed to update user {UserName}. Errors: {Errors}", user.UserName, result.Errors);
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
            logger.LogError("Failed to delete user {UserName}. Errors: {Errors}", user.UserName, result.Errors);
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

        var existingFollow = await context.UserFollows
            .FirstOrDefaultAsync(uf => uf.FollowerId == followerId && uf.FollowingId == followingId);

        if (existingFollow == null)
        {
            context.UserFollows.Add(new UserFollow { FollowerId = followerId, FollowingId = followingId });
            await context.SaveChangesAsync();
            logger.LogInformation("User {FollowerId} followed user {FollowingId}.", followerId, followingId);
            return true;
        }

        logger.LogWarning("User {FollowerId} is already following user {FollowingId}.", followerId, followingId);
        return false;
    }

    // Unfollow a user
    public async Task<bool> UnfollowUserAsync(string followerId, string followingId)
    {
        var follow = await context.UserFollows
            .FirstOrDefaultAsync(uf => uf.FollowerId == followerId && uf.FollowingId == followingId);

        if (follow != null)
        {
            context.UserFollows.Remove(follow);
            await context.SaveChangesAsync();
            logger.LogInformation("User {FollowerId} unfollowed user {FollowingId}.", followerId, followingId);
            return true;
        }

        logger.LogWarning("User {FollowerId} is not following user {FollowingId}.", followerId, followingId);
        return false;
    }

    // Get followers of a user
    public async Task<List<UserModel>> GetFollowersAsync(string userId)
    {
        var followers = await context.UserFollows
            .Where(uf => uf.FollowingId == userId)
            .Select(uf => uf.Follower)
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
            .ToListAsync();

        logger.LogInformation("User {UserId} is following {FollowingCount} users.", userId, following.Count);
        return following.Select(MapToUserModel).ToList();
    }

    // Private method to map from EF entity to UserModel
    private static UserModel MapToUserModel(ShareSmallBizUser user)
    {
        return new UserModel
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfilePicture = user.ProfilePicture,
            ProfilePictureUrl = user.ProfilePictureUrl,
            PostCount = user.Posts?.Count ?? 0,
            LikeCount = user.LikedPosts?.Count ?? 0
        };
    }
}
