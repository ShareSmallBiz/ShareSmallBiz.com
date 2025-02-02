using Microsoft.EntityFrameworkCore;
using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Infrastructure.Services
{
    public class PostService
    {
        private readonly ShareSmallBizUserContext _context;

        public PostService(ShareSmallBizUserContext context)
        {
            _context = context;
        }

        public async Task<bool> LikePostAsync(string userId, int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return false;

            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(pl => pl.UserId == userId && pl.PostId == postId);

            if (existingLike == null)
            {
                _context.PostLikes.Add(new PostLike { UserId = userId, PostId = postId });
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}

