namespace ShareSmallBiz.Portal.Infrastructure.Services;

public interface IPostProvider
{
    Task<PostModel> CreatePostAsync(PostModel postModel);
    Task<bool> DeletePostAsync(int postId);
    Task<List<PostModel>> GetAllPostsAsync(bool onlyPublic = true);
    Task<PostModel?> GetPostByIdAsync(int postId);
    Task<bool> UpdatePostAsync(PostModel postModel);
    Task<List<PostModel>> MostRecentPostsAsync(int count);
    Task<List<PostModel>> MostPopularPostsAsync(int count);
    Task<List<PostModel>> GetPostsAsync(int perPage, int pageNumber);
    Task SeedPostsAsync();
}
