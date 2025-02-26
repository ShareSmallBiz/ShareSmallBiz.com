using ShareSmallBiz.Portal.Data;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace ShareSmallBiz.Portal.Infrastructure.Services;



public class DiscussionProvider(
    ShareSmallBizUserContext context,
    ILogger<DiscussionProvider> logger,
    UserManager<ShareSmallBizUser> userManager,
     IHttpContextAccessor httpContextAccessor)
{
    public static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "UNKNOWN";
        }

        // Normalize the string
        string normalized = title.Normalize(NormalizationForm.FormD);

        // Remove diacritic marks (accents)
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        string cleaned = sb.ToString().Normalize(NormalizationForm.FormC);

        // Convert to lowercase
        cleaned = cleaned.ToLowerInvariant();

        // Replace spaces with dashes
        cleaned = Regex.Replace(cleaned, @"\s+", "-");

        // Remove invalid URL characters
        cleaned = Regex.Replace(cleaned, @"[^a-z0-9\-]", "");

        // Trim dashes from start and end
        cleaned = cleaned.Trim('-');

        if (string.IsNullOrEmpty(cleaned))
        {
            cleaned = "unknown";
        }
        return cleaned;
    }

    public static string StringToHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        return input.Replace("\r\n", "<br/>").Replace("\n", "<br/>");
    }


    /// <summary>
    /// Allows a logged-in user to comment on a post.
    /// </summary>
    /// <param name="postId">ID of the post to comment on.</param>
    /// <param name="comment">The comment text.</param>
    /// <param name="userPrincipal">The current user's claims principal.</param>
    /// <returns>True if the comment was added successfully, otherwise false.</returns>
    public async Task<bool> CommentPostAsync(int postId, string comment, ClaimsPrincipal userPrincipal)
    {
        // Validate user
        var user = await userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting comment creation.");
            return false;
        }

        logger.LogInformation("User {UserId} adding comment to post with ID: {PostId}", user.Id, postId);

        var post = await context.Posts.FindAsync(postId);
        if (post == null)
        {
            logger.LogWarning("Post with ID {PostId} not found.", postId);
            return false;
        }

        // Create new comment
        var postComment = new PostComment
        {
            PostId = postId,
            Content = comment,
            CreatedDate = DateTime.UtcNow,
            Author = user // Or store user.Id separately if you prefer
        };

        context.PostComments.Add(postComment);
        await context.SaveChangesAsync();
        return true;
    }



    /// <inheritdoc/>
    public async Task<DiscussionModel?> CreatePostAsync(DiscussionModel discussionModel, ClaimsPrincipal userPrincipal)
    {
        var user = await userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting post creation.");
            return null;
        }

        logger.LogInformation("Creating a new post with title: {Title}", discussionModel.Title);
        var post = new Post
        {
            Title = discussionModel.Title,
            Content = discussionModel.Content,
            Description = discussionModel.Description,
            Cover = discussionModel.Cover,
            IsFeatured = discussionModel.IsFeatured,
            IsPublic = discussionModel.IsPublic,
            PostType = discussionModel.PostType,
            PostViews = discussionModel.PostViews,
            Published = discussionModel.Published,
            Rating = discussionModel.Rating,
            Selected = discussionModel.Selected,
            Slug = GenerateSlug(discussionModel.Title),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedID = user.Id,
            ModifiedID = user.Id,
            AuthorId = user.Id
        };

        context.Posts.Add(post);
        await context.SaveChangesAsync();
        return new DiscussionModel(post);
    }

    /// <inheritdoc/>
    public async Task<bool> DeletePostAsync(int postId)
    {
        logger.LogInformation("Deleting post with ID: {PostId}", postId);
        var post = await context.Posts.FindAsync(postId);
        if (post == null)
            return false;

        context.Posts.Remove(post);
        await context.SaveChangesAsync();
        return true;
    }
    public async Task<List<DiscussionModel>> GetAllUserPostsAsync(bool onlyPublic = true)
    {
        // Get The Current User Id
        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        logger.LogInformation("Retrieving all posts. Only public: {OnlyPublic}", onlyPublic);
        var posts = await context.Posts
            .Where(p => !onlyPublic || p.IsPublic)
            .Where(p => p.AuthorId == userId)
            .Include(p => p.Author)
            .Include(p => p.PostCategories)
            .ToListAsync();

        return [.. posts.Select(p => new DiscussionModel(p))];
    }
    /// <inheritdoc/>
    public async Task<List<DiscussionModel>> GetAllPostsAsync(bool onlyPublic = true)
    {
        logger.LogInformation("Retrieving all posts. Only public: {OnlyPublic}", onlyPublic);
        var posts = await context.Posts
            .Where(p => !onlyPublic || p.IsPublic)
            .Include(p => p.Author)
            .Include(p => p.PostCategories)
            .ToListAsync();

        return [.. posts.Select(p => new DiscussionModel(p))];
    }

    public async Task<DiscussionModel?> GetPostByIdAsync(int postId)
    {
        logger.LogInformation("Retrieving post with ID: {PostId}", postId);

        // Fetch the post with all related data
        var post = await context.Posts
            .Include(p => p.Author)
            .Include(p => p.PostCategories)
            .Include(p => p.Likes).ThenInclude(l => l.User)
            .Include(p => p.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            return null;
        }

        // Increment the view count (even if it fails due to concurrency conflict)
        post.PostViews++;

        // Try saving the changes
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Log the error but don't throw or interrupt the flow
            logger.LogWarning("Concurrency conflict occurred while updating post view count for PostId: {PostId}. Error: {Message}", postId, ex.Message);
            // Optionally, you can log more details about the exception if needed
        }

        // Map to DiscussionModel for return
        DiscussionModel returnPost = new(post);
        returnPost.Content = StringToHtml(returnPost.Content);

        return returnPost;
    }


    /// <inheritdoc/>
    public async Task<List<DiscussionModel>> GetPostsAsync(int perPage, int pageNumber)
    {
        logger.LogInformation("Retrieving page {PageNumber} with {PerPage} posts per page", pageNumber, perPage);

        var posts = await context.Posts
            .Where(p => p.IsPublic)
            .OrderByDescending(p => p.Published)
            .Skip((pageNumber - 1) * perPage)
            .Take(perPage)
            .AsNoTracking()
            .ToListAsync();

        return [.. posts.Select(p => new DiscussionModel(p))];
    }

    /// <summary>
    /// Retrieves a paginated and optionally sorted set of posts.
    /// </summary>
    public async Task<PaginatedPostResult> GetPostsAsync(int pageNumber, int pageSize, SortType sortType)
    {
        logger.LogInformation("Retrieving posts with sort type: {SortType}, page number: {PageNumber}, page size: {PageSize}",
            sortType, pageNumber, pageSize);

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        IQueryable<Post> query = context.Posts.Include(i => i.Author).Where(p => p.IsPublic);

        switch (sortType)
        {
            case SortType.Recent:
                query = query.OrderByDescending(p => p.Published);
                break;
            case SortType.Popular:
                query = query.OrderByDescending(p => p.PostViews);
                break;
            default:
                query = query.OrderByDescending(p => p.Id);
                break;
        }

        var totalCount = await query.CountAsync();

        var posts = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return new PaginatedPostResult
        {
            Posts = [.. posts.Select(p => new DiscussionModel(p))],
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }


    /// <summary>
    /// Allows a logged-in user to "like" a post.
    /// </summary>
    /// <param name="postId">ID of the post to like.</param>
    /// <param name="userPrincipal">The current user's claims principal.</param>
    /// <returns>True if the post was successfully liked, otherwise false.</returns>
    public async Task<bool> LikePostAsync(int postId, ClaimsPrincipal userPrincipal)
    {
        // Validate user
        var user = await userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting like operation.");
            return false;
        }

        logger.LogInformation("User {UserId} is liking post with ID: {PostId}", user.Id, postId);

        var post = await context.Posts.FindAsync(postId);
        if (post == null)
        {
            logger.LogWarning("Post with ID {PostId} not found.", postId);
            return false;
        }

        // Check if user already liked the post
        var postLike = await context.PostLikes
            .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == user.Id);

        if (postLike != null)
        {
            logger.LogInformation("User {UserId} has already liked post {PostId}.", user.Id, postId);
            return false;
        }

        // Create new like
        postLike = new PostLike
        {
            PostId = postId,
            UserId = user.Id
        };

        context.PostLikes.Add(postLike);
        await context.SaveChangesAsync();
        return true;
    }
    /// <inheritdoc/>
    public async Task<List<DiscussionModel>> FeaturedPostsAsync(int count)
    {
        logger.LogInformation("Retrieving {Count} featured posts", count);
        var posts = await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments).ThenInclude(c => c.Author)
            .Where(p => p.IsPublic)
            .Where(p => p.IsFeatured)
            .OrderByDescending(p => p.Comments.Count)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();

        return [.. posts.Select(p => new DiscussionModel(p))];
    }

    /// <inheritdoc/>
    public async Task<List<DiscussionModel>> MostCommentedPostsAsync(int count)
    {
        logger.LogInformation("Retrieving {Count} most commented posts", count);
        var posts = await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments).ThenInclude(c => c.Author)
            .Where(p => p.IsPublic)
            .OrderByDescending(p => p.Comments.Count)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();

        return [.. posts.Select(p => new DiscussionModel(p))];
    }

    /// <inheritdoc/>
    public async Task<List<DiscussionModel>> MostPopularPostsAsync(int count)
    {
        logger.LogInformation("Retrieving {Count} most popular posts", count);
        var posts = await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments).ThenInclude(c => c.Author)
            .Where(p => p.IsPublic)
            .OrderByDescending(p => p.PostViews)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();

        return [.. posts.Select(p => new DiscussionModel(p))];
    }

    /// <inheritdoc/>
    public async Task<List<DiscussionModel>> MostRecentPostsAsync(int count)
    {
        logger.LogInformation("Retrieving {Count} most recent posts", count);
        var posts = await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments).ThenInclude(c => c.Author)
            .Where(p => p.IsPublic)
            .OrderByDescending(p => p.Published)
            .Take(count)
            .AsNoTracking()
            .ToListAsync().ConfigureAwait(false);

        return [.. posts.Select(p => new DiscussionModel(p))];
    }

    /// <inheritdoc/>
    public async Task<bool> UpdatePostAsync(DiscussionModel discussionModel, ClaimsPrincipal userPrincipal, CancellationToken ct = default)
    {
        var user = await userManager.GetUserAsync(userPrincipal).ConfigureAwait(false);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting post update.");
            return false;
        }
        logger.LogInformation("Updating post with ID: {PostId}", discussionModel.Id);

        var existingPost = await context.Posts.
            Where(p => p.Id == discussionModel.Id)
            .Include(discussionModel => discussionModel.PostCategories)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        if (existingPost == null)
            return false;

        existingPost.Title = discussionModel.Title;
        existingPost.Content = discussionModel.Content;
        existingPost.Description = discussionModel.Description;
        existingPost.Cover = discussionModel.Cover;
        existingPost.IsFeatured = discussionModel.IsFeatured;
        existingPost.IsPublic = discussionModel.IsPublic;
        existingPost.PostType = discussionModel.PostType;
        existingPost.Published = discussionModel.Published;
        existingPost.Selected = discussionModel.Selected;
        existingPost.Slug = GenerateSlug(discussionModel.Title);
        existingPost.ModifiedDate = DateTime.UtcNow;
        existingPost.ModifiedID = user.Id;

        // Update post categories
        existingPost.PostCategories.Clear();
        foreach (var categoryName in discussionModel.Tags)
        {
            var category = await context.Keywords
                .FirstOrDefaultAsync(k => k.Name == categoryName, ct).ConfigureAwait(false);
            if (category != null)
            {
                existingPost.PostCategories.Add(category);
            }
        }
        context.Posts.Update(existingPost);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
    /// <summary>
    /// Allows a logged-in user to delete a comment on a post.
    /// </summary>
    /// <param name="postId">ID of the post containing the comment.</param>
    /// <param name="commentId">ID of the comment to delete.</param>
    /// <param name="userPrincipal">The current user's claims principal.</param>
    /// <returns>True if the comment was deleted successfully, otherwise false.</returns>
    public async Task<bool> DeleteCommentAsync(int postId, int commentId, ClaimsPrincipal userPrincipal, CancellationToken ct)
    {
        // Validate user
        var user = await userManager.GetUserAsync(userPrincipal).ConfigureAwait(false);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting comment deletion.");
            return false;
        }

        logger.LogInformation("User {UserId} attempting to delete comment with ID: {CommentId} from post with ID: {PostId}", user.Id, commentId, postId);

        var comment = await context.PostComments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.PostId == postId, ct).ConfigureAwait(false);

        if (comment == null)
        {
            logger.LogWarning("Comment with ID {CommentId} not found in post with ID {PostId}.", commentId, postId);
            return false;
        }

        if (string.Compare(comment.Author.Id, user.Id, StringComparison.Ordinal) != 0)
        {
            logger.LogWarning("User {UserId} attempted to delete a comment they do not own.", user.Id);
            return false;
        }

        context.PostComments.Remove(comment);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<List<DiscussionModel>> GetPostsByTagAsync(string id)
    {
        logger.LogInformation("Retrieving posts by tag: {Tag}", id);
        var posts = await context.Posts
            .Include(p => p.Author)
            .Include(p => p.PostCategories)
            .Where(p => p.IsPublic)
            .Where(p => p.PostCategories.Any(c => c.Name == id))
            .AsNoTracking()
            .Select(p => new DiscussionModel(p))
            .ToListAsync().ConfigureAwait(true);

        return posts ?? new List<DiscussionModel>();
    }
}
