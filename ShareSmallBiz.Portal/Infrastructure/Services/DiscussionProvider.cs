using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;
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
        cleaned = Regex.Replace(cleaned, @"[^a-z0-9\-]", string.Empty);

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
    /// Allows a logged-in user to comment on a discussion.
    /// </summary>
    /// <param name="id">ID of the discussion to comment on.</param>
    /// <param name="comment">The comment text.</param>
    /// <param name="userPrincipal">The current user's claims principal.</param>
    /// <returns>True if the comment was added successfully, otherwise false.</returns>
    public async Task<bool> DiscussionCommentPostAsync(int id, string comment, ClaimsPrincipal userPrincipal)
    {
        // Validate user
        var user = await userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting comment creation.");
            return false;
        }

        logger.LogInformation("User {CreatedID} adding comment to discussion with ID: {id}", user.Id, id);

        var post = await context.Posts.FindAsync(id);
        if (post == null)
        {
            logger.LogWarning("Discussion with ID {id} not found.", id);
            return false;
        }

        // Create new comment
        var postComment = new PostComment
        {
            PostId = id,
            Content = comment,
            CreatedDate = DateTime.UtcNow,
            CreatedID = user.Id
        };

        context.PostComments.Add(postComment);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<DiscussionModel?> CreateDiscussionAsync(DiscussionModel discussionModel, ClaimsPrincipal userPrincipal)
    {
        var user = await userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting discussion creation.");
            return null;
        }

        logger.LogInformation("Creating a new discussion with title: {Title}", discussionModel.Title);
        var discussion = new Post
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
            CreatedID = user.Id,
            CreatedDate = DateTime.UtcNow,
            ModifiedID = user.Id,
            ModifiedDate = DateTime.UtcNow
        };

        context.Posts.Add(discussion);
        await context.SaveChangesAsync();
        return new DiscussionModel(discussion);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteDiscussionAsync(int id)
    {
        logger.LogInformation("Deleting discussion with ID: {id}", id);
        var discussion = await context.Posts.FindAsync(id);
        if (discussion == null)
            return false;

        context.Posts.Remove(discussion);
        await context.SaveChangesAsync();
        return true;
    }
    public async Task<List<DiscussionModel>> GetAllUserDiscussionsAsync(bool onlyPublic = true)
    {
        // Get The Current User Id
        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        logger.LogInformation("Retrieving all posts. Only public: {OnlyPublic}", onlyPublic);
        var posts = await context.Posts
            .Where(p => !onlyPublic || p.IsPublic)
            .Where(p => p.CreatedID == userId)
            .Include(p => p.Creator)
            .Include(p => p.PostCategories)
            .ToListAsync();

        return [.. posts.Select(p => new DiscussionModel(p))];
    }
    /// <inheritdoc/>
    public async Task<List<DiscussionModel>> GetAllDiscussionsAsync(bool onlyPublic = true)
    {
        logger.LogInformation("Retrieving all discussions. Only public: {OnlyPublic}", onlyPublic);
        var posts = await context.Posts
            .Where(p => !onlyPublic || p.IsPublic)
            .Include(p => p.Creator)
            .Include(p => p.PostCategories)
            .ToListAsync();

        return [.. posts.Select(p => new DiscussionModel(p))];
    }

    public async Task<DiscussionModel?> GetPostByIdAsync(int id)
    {
        logger.LogInformation("Retrieving discussion with ID: {id}", id);

        // Fetch the discussion with all related data
        var post = await context.Posts
            .Include(p => p.Creator)
            .Include(p => p.PostCategories)
            .Include(p => p.Likes).ThenInclude(l => l.Creator)
            .Include(p => p.Comments).ThenInclude(c => c.Creator)
            .FirstOrDefaultAsync(p => p.Id == id);

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
            logger.LogWarning("Concurrency conflict occurred while updating discussion view count for PostId: {id}. Error: {Message}", id, ex.Message);
            // Optionally, you can log more details about the exception if needed
        }

        // Map to DiscussionModel for return
        DiscussionModel returnDiscussion = new(post);
        returnDiscussion.Content = StringToHtml(returnDiscussion.Content);

        return returnDiscussion;
    }


    /// <inheritdoc/>
    public async Task<List<DiscussionModel>> GetDiscussionsAsync(int perPage, int pageNumber)
    {
        logger.LogInformation("Retrieving page {PageNumber} with {PerPage} discussions per page", pageNumber, perPage);

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
    public async Task<PaginatedPostResult> GetDiscussionsAsync(int pageNumber, int pageSize, SortType sortType)
    {
        logger.LogInformation("Retrieving posts with sort type: {SortType}, page number: {PageNumber}, page size: {PageSize}",
            sortType, pageNumber, pageSize);

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        IQueryable<Post> query = context.Posts.Include(i => i.Creator).Where(p => p.IsPublic);

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
    /// Allows a logged-in user to "like" a discussion.
    /// </summary>
    /// <param name="postId">ID of the discussion to like.</param>
    /// <param name="userPrincipal">The current user's claims principal.</param>
    /// <returns>True if the discussion was successfully liked, otherwise false.</returns>
    public async Task<bool> LikePostAsync(int postId, ClaimsPrincipal userPrincipal)
    {
        // Validate user
        var user = await userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting like operation.");
            return false;
        }

        logger.LogInformation("User {CreatedID} is liking discussion with ID: {PostId}", user.Id, postId);

        var post = await context.Posts.FindAsync(postId);
        if (post == null)
        {
            logger.LogWarning("Post with ID {PostId} not found.", postId);
            return false;
        }

        // Check if user already liked the discussion
        var postLike = await context.PostLikes
            .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.CreatedID == user.Id);

        if (postLike != null)
        {
            logger.LogInformation("User {CreatedID} has already liked discussion {PostId}.", user.Id, postId);
            return false;
        }

        // Create new like
        postLike = new PostLike
        {
            PostId = postId,
            CreatedID = user.Id
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
            .Include(p => p.Creator)
            .Include(p => p.Comments).ThenInclude(c => c.Creator)
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
            .Include(p => p.Creator)
            .Include(p => p.Comments).ThenInclude(c => c.Creator)
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
            .Include(p => p.Creator)
            .Include(p => p.Comments).ThenInclude(c => c.Creator)
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
            .Include(p => p.Creator)
            .Include(p => p.Comments).ThenInclude(c => c.Creator)
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
        try
        {

            var user = await userManager.GetUserAsync(userPrincipal).ConfigureAwait(false);
            if (user == null)
            {
                logger.LogWarning("No logged-in user found. Aborting discussion update.");
                return false;
            }
            logger.LogInformation("Updating discussion with ID: {PostId}", discussionModel.Id);

            var existingPost = await context.Posts.
                Where(p => p.Id == discussionModel.Id)
                .Include(discussionModel => discussionModel.PostCategories)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);

            if (existingPost == null)
                return false;

            if (string.IsNullOrEmpty(discussionModel?.Author?.Id))
            {
                discussionModel.Author = new UserModel(user);
                existingPost.CreatedID = discussionModel.Author.Id;
            }

            if (discussionModel.Author != null && string.IsNullOrEmpty(discussionModel.Author.Id))
            {
                discussionModel.Author.Id = user.Id;
            }
            else
            {
                existingPost.CreatedID = discussionModel.Author?.Id ?? user.Id;
            }
            if (discussionModel.CreatedID == null || string.IsNullOrEmpty(discussionModel.CreatedID))
            {
                discussionModel.CreatedID = user.Id;
            }
            else
            {
                existingPost.CreatedID = discussionModel.CreatedID;
            }
            existingPost.ModifiedID = user.Id;


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

            existingPost.PostCategories.Clear();

            if (discussionModel.Tags.Count > 0)
            {
                foreach (var categoryName in discussionModel.Tags)
                {
                    var category = await context.Keywords
                        .FirstOrDefaultAsync(k => k.Name == categoryName, ct).ConfigureAwait(false);
                    if (category != null)
                    {
                        existingPost.PostCategories.Add(category);
                    }
                }
            }
            context.Posts.Update(existingPost);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating discussion with ID: {PostId}.", discussionModel.Id);

            // find inner exception if any
            if (ex.InnerException != null)
            {
                logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException.Message);
            }


        }
        return false;
    }
    /// <summary>
    /// Allows a logged-in user to delete a comment on a discussion.
    /// </summary>
    /// <param name="postId">ID of the discussion containing the comment.</param>
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

        logger.LogInformation("User {CreatedID} attempting to delete comment with ID: {CommentId} from discussion with ID: {PostId}", user.Id, commentId, postId);

        var comment = await context.PostComments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.PostId == postId, ct).ConfigureAwait(false);

        if (comment == null)
        {
            logger.LogWarning("Comment with ID {CommentId} not found in discussion with ID {PostId}.", commentId, postId);
            return false;
        }

        if (string.Compare(comment.CreatedID, user.Id, StringComparison.Ordinal) != 0)
        {
            logger.LogWarning("User {CreatedID} attempted to delete a comment they do not own.", user.Id);
            return false;
        }

        context.PostComments.Remove(comment);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<List<DiscussionModel>> GetDiscussionsByTagAsync(string id, bool onlyPublic = true)
    {
        logger.LogInformation("Retrieving posts by tag: {Tag}", id);
        var posts = await context.Posts
            .Where(p => !onlyPublic || p.IsPublic)
            .Include(p => p.Creator)
            .Include(p => p.PostCategories)
            .Where(p => p.IsPublic)
            .Where(p => p.PostCategories.Any(c => c.Name == id))
            .AsNoTracking()
            .Select(p => new DiscussionModel(p))
            .ToListAsync().ConfigureAwait(true);

        return posts ?? new List<DiscussionModel>();
    }
}
