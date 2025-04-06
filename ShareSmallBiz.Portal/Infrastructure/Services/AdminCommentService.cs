using global::ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;

namespace ShareSmallBiz.Portal.Infrastructure.Services;


public class AdminCommentService
{
    private readonly ShareSmallBizUserContext _context;
    private readonly ILogger<AdminCommentService> _logger;
    private readonly UserManager<ShareSmallBizUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminCommentService(
        ShareSmallBizUserContext context,
        ILogger<AdminCommentService> logger,
        UserManager<ShareSmallBizUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Creates a new comment as an admin. Returns the created comment as a view model or null if unsuccessful.
    /// </summary>
    public async Task<PostCommentModel?> CreateCommentAsync(int postId, string content)
    {
        // Ensure the current user is an admin
        var adminUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        if (adminUser == null || !await _userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            _logger.LogWarning("Unauthorized admin attempt to create a comment.");
            return null;
        }

        // Retrieve the related post
        var post = await _context.Posts.FindAsync(postId);
        if (post == null)
        {
            _logger.LogWarning("Post with ID {PostId} not found.", postId);
            return null;
        }

        // Create the new comment. Here, we assume that PostComment has a constructor or assignment that accepts a user.
        var comment = new PostComment
        {
            PostId = postId,
            Content = content,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            // Here we mimic the pattern in your provided code; adjust as necessary.
            CreatedID = adminUser.Id
        };

        _context.PostComments.Add(comment);
        await _context.SaveChangesAsync();

        return new PostCommentModel(comment);
    }

    /// <summary>
    /// Retrieves a comment by its ID.
    /// </summary>
    public async Task<PostCommentModel?> GetCommentByIdAsync(int commentId)
    {
        var comment = await _context.PostComments
            .Include(c => c.Creator)
            .Include(c => c.Likes)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
        {
            _logger.LogWarning("Comment with ID {CommentId} not found.", commentId);
            return null;
        }

        return new PostCommentModel(comment);
    }

    /// <summary>
    /// Retrieves all comments for moderation. You can modify this to include pagination or filtering as needed.
    /// </summary>
    public async Task<List<PostCommentModel>> GetAllCommentsAsync()
    {
        var comments = await _context.PostComments
            .Include(c => c.Creator)
            .Include(c => c.Likes)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();

        return comments.Select(c => new PostCommentModel(c)).ToList();
    }

    /// <summary>
    /// Updates a comment's content. This method bypasses ownership checks since it is for admin moderation.
    /// </summary>
    public async Task<bool> UpdateCommentAsync(int commentId, string updatedContent)
    {
        // Check that the user is an admin.
        var adminUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        if (adminUser == null || !await _userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            _logger.LogWarning("Unauthorized admin attempt to update comment.");
            return false;
        }

        var comment = await _context.PostComments.FindAsync(commentId);
        if (comment == null)
        {
            _logger.LogWarning("Comment with ID {CommentId} not found.", commentId);
            return false;
        }

        comment.Content = updatedContent;
        comment.ModifiedDate = DateTime.UtcNow;
        // Optionally update ModifiedID: comment.ModifiedID = adminUser.Id;

        _context.PostComments.Update(comment);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Deletes a comment by its ID. This operation is allowed for admin users regardless of comment ownership.
    /// </summary>
    public async Task<bool> DeleteCommentAsync(int commentId)
    {
        // Check that the current user is an admin.
        var adminUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        if (adminUser == null || !await _userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            _logger.LogWarning("Unauthorized admin attempt to delete comment.");
            return false;
        }

        var comment = await _context.PostComments.FindAsync(commentId);
        if (comment == null)
        {
            _logger.LogWarning("Comment with ID {CommentId} not found.", commentId);
            return false;
        }

        _context.PostComments.Remove(comment);
        await _context.SaveChangesAsync();
        return true;
    }
}

