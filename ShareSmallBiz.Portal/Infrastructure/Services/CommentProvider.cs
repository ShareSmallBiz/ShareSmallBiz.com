using ShareSmallBiz.Portal.Data;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class CommentProvider(
    ShareSmallBizUserContext context,
    ILogger<CommentProvider> logger,
    UserManager<ShareSmallBizUser> userManager,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<bool> AddCommentAsync(int postId, string content, ClaimsPrincipal userPrincipal)
    {
        var user = await userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting comment creation.");
            return false;
        }

        var post = await context.Posts.FindAsync(postId);
        if (post == null)
        {
            logger.LogWarning("Post with ID {PostId} not found.", postId);
            return false;
        }

        var postComment = new PostComment
        {
            PostId = postId,
            Content = content,
            CreatedDate = DateTime.UtcNow,
            Author = new(user)
        };

        context.PostComments.Add(postComment);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateCommentAsync(int commentId, string updatedContent, ClaimsPrincipal userPrincipal)
    {
        var user = await userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting comment update.");
            return false;
        }

        var comment = await context.PostComments.FindAsync(commentId);
        if (comment == null)
        {
            logger.LogWarning("Comment with ID {CommentId} not found.", commentId);
            return false;
        }

        if (comment.Author.Id != user.Id)
        {
            logger.LogWarning("User {UserId} attempted to update a comment they do not own.", user.Id);
            return false;
        }

        comment.Content = updatedContent;
        comment.ModifiedDate = DateTime.UtcNow;

        context.PostComments.Update(comment);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCommentAsync(int commentId, ClaimsPrincipal userPrincipal)
    {
        var user = await userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting comment deletion.");
            return false;
        }

        var comment = await context.PostComments.FindAsync(commentId);
        if (comment == null)
        {
            logger.LogWarning("Comment with ID {CommentId} not found.", commentId);
            return false;
        }

        if (comment.Author.Id != user.Id)
        {
            logger.LogWarning("User {UserId} attempted to delete a comment they do not own.", user.Id);
            return false;
        }

        context.PostComments.Remove(comment);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<PostComment>> GetCommentsForPostAsync(int postId)
    {
        return await context.PostComments
            .Where(c => c.PostId == postId)
            .Include(c => c.Author)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();
    }
}
