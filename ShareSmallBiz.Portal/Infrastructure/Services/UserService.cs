using Microsoft.EntityFrameworkCore;
using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Infrastructure.Services
{
    public class UserService
    {
        private readonly ShareSmallBizUserContext _context;

        public UserService(ShareSmallBizUserContext context)
        {
            _context = context;
        }

        public async Task<bool> FollowUserAsync(string followerId, string followingId)
        {
            if (followerId == followingId) return false;

            var existingFollow = await _context.UserFollows
                .FirstOrDefaultAsync(uf => uf.FollowerId == followerId && uf.FollowingId == followingId);

            if (existingFollow == null)
            {
                _context.UserFollows.Add(new UserFollow { FollowerId = followerId, FollowingId = followingId });
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}

