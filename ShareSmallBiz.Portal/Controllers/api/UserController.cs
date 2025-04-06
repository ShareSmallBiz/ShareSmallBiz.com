using Microsoft.AspNetCore.Authentication.JwtBearer;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers.api;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/[controller]")]
public class UsersController(UserProvider userService, ILogger<UsersController> logger) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> CreateUser([FromBody] UserModel model, [FromQuery] string password)
    {
        logger.LogInformation("Received request to create user {UserName}", model.UserName);
        var result = await userService.CreateUserAsync(model, password);
        return result ? Ok("User created successfully") : BadRequest("Failed to create user");
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserById(string userId)
    {
        logger.LogInformation("Received request to get user by ID {CreatedID}", userId);
        var user = await userService.GetUserByIdAsync(userId);
        return user != null ? Ok(user) : NotFound($"User with ID {userId} not found");
    }

    [HttpGet("username/{username}")]
    public async Task<IActionResult> GetUserByUsername(string username)
    {
        logger.LogInformation("Received request to get user by username {Username}", username);
        var user = await userService.GetUserByUsernameAsync(username);
        return user != null ? Ok(user) : NotFound($"User with username {username} not found");
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsers()
    {
        logger.LogInformation("Received request to get all users");
        var users = await userService.GetAllPublicUsersAsync();
        return Ok(users);
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] UserModel model)
    {
        logger.LogInformation("Received request to update user {CreatedID}", userId);
        var result = await userService.UpdateUserAsync(userId, model);
        return result ? Ok("User updated successfully") : NotFound($"User with ID {userId} not found");
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        logger.LogInformation("Received request to delete user {CreatedID}", userId);
        var result = await userService.DeleteUserAsync(userId);
        return result ? Ok("User deleted successfully") : NotFound($"User with ID {userId} not found");
    }

    [HttpPost("{followerId}/follow/{followingId}")]
    public async Task<IActionResult> FollowUser(string followerId, string followingId)
    {
        logger.LogInformation("Received request for user {CreatedID} to follow {FollowingId}", followerId, followingId);
        var result = await userService.FollowUserAsync(followerId, followingId);
        return result ? Ok("User followed successfully") : BadRequest("Follow request failed");
    }

    [HttpPost("{followerId}/unfollow/{followingId}")]
    public async Task<IActionResult> UnfollowUser(string followerId, string followingId)
    {
        logger.LogInformation("Received request for user {CreatedID} to unfollow {FollowingId}", followerId, followingId);
        var result = await userService.UnfollowUserAsync(followerId, followingId);
        return result ? Ok("User unfollowed successfully") : BadRequest("Unfollow request failed");
    }

    [HttpGet("{userId}/followers")]
    public async Task<IActionResult> GetFollowers(string userId)
    {
        logger.LogInformation("Received request to get followers of user {CreatedID}", userId);
        var followers = await userService.GetFollowersAsync(userId);
        return Ok(followers);
    }

    [HttpGet("{userId}/following")]
    public async Task<IActionResult> GetFollowing(string userId)
    {
        logger.LogInformation("Received request to get following users of {CreatedID}", userId);
        var following = await userService.GetFollowingAsync(userId);
        return Ok(following);
    }
}
