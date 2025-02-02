namespace ShareSmallBiz.Portal.Infrastructure.Services
{
    public interface IPostProvider
    {
        Task<PostModel> CreatePostAsync(PostModel postModel);
        Task<bool> DeletePostAsync(int postId);
        Task<List<PostModel>> GetAllPostsAsync(bool onlyPublic = true);
        Task<PostModel?> GetPostByIdAsync(int postId);
        Task<bool> UpdatePostAsync(PostModel postModel);
    }
}

