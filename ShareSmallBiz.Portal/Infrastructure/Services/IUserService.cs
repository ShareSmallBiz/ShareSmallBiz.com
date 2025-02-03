namespace ShareSmallBiz.Portal.Infrastructure.Services
{
    public interface IUserService
    {
        Task<bool> CreateUserAsync(UserModel model, string password);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> FollowUserAsync(string followerId, string followingId);
        Task<List<UserModel>> GetAllUsersAsync();
        Task<List<UserModel>> GetFollowersAsync(string userId);
        Task<List<UserModel>> GetFollowingAsync(string userId);
        Task<UserModel?> GetUserByIdAsync(string userId);
        Task<UserModel?> GetUserByUsernameAsync(string username);
        Task<bool> UnfollowUserAsync(string followerId, string followingId);
        Task<bool> UpdateUserAsync(string userId, UserModel model);
    }
}
