﻿using ShareSmallBiz.Portal.Data;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Infrastructure.Services;


public class PostProvider(
    ShareSmallBizUserContext context,
    ILogger<PostProvider> logger,
    UserManager<ShareSmallBizUser> userManager,
     IHttpContextAccessor httpContextAccessor) 
{
    /// <summary>
    /// Allows a logged-in user to add a new comment to a post specified by ID.
    /// </summary>
    /// <param name="postId">ID of the post to comment on.</param>
    /// <param name="comment">The comment's text.</param>
    /// <returns>True if the comment was successfully added, otherwise false.</returns>
    public async Task<bool> CommentPostAsync(int postId, string comment)
    {
        try
        {
            var userPrincipal = httpContextAccessor.HttpContext?.User;
            if (userPrincipal is null || !userPrincipal.Identity?.IsAuthenticated == true)
            {
                logger.LogWarning("No authenticated user found in HttpContext. Aborting comment operation.");
                return false;
            }

            // Get the user from the identity system
            var user = await userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                logger.LogWarning("UserManager could not find a valid user. Aborting comment operation.");
                return false;
            }

            logger.LogInformation("User {UserId} attempting to comment on post with ID: {PostId}", user.Id, postId);

            var post = await context.Posts.FindAsync(postId);
            if (post == null)
            {
                logger.LogWarning("Post with ID {PostId} not found.", postId);
                return false;
            }

            // Create the new comment
            var postComment = new PostComment
            {
                PostId = postId,
                Content = comment,
                CreatedDate = DateTime.UtcNow,
                Author = user  // or store user.Id if you prefer
            };

            context.PostComments.Add(postComment);
            await context.SaveChangesAsync();

            logger.LogInformation("User {UserId} successfully added a comment to post {PostId}.", user.Id, postId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while commenting on post {PostId}.", postId);
            return false;
        }
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
    public async Task<PostModel?> CreatePostAsync(PostModel postModel, ClaimsPrincipal userPrincipal)
    {
        var user = await userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting post creation.");
            return null;
        }

        logger.LogInformation("Creating a new post with title: {Title}", postModel.Title);
        var post = new Post
        {
            Title = postModel.Title,
            Content = postModel.Content,
            Description = postModel.Description,
            Cover = postModel.Cover,
            IsFeatured = postModel.IsFeatured,
            IsPublic = postModel.IsPublic,
            PostType = postModel.PostType,
            PostViews = postModel.PostViews,
            Published = postModel.Published,
            Rating = postModel.Rating,
            Selected = postModel.Selected,
            Slug = postModel.Slug,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedID = user.Id,
            ModifiedID = user.Id,
            AuthorId = user.Id
        };

        context.Posts.Add(post);
        await context.SaveChangesAsync();
        return new PostModel(post);
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

    /// <inheritdoc/>
    public async Task<List<PostModel>> GetAllPostsAsync(bool onlyPublic = true)
    {
        logger.LogInformation("Retrieving all posts. Only public: {OnlyPublic}", onlyPublic);
        var posts = await context.Posts
            .Where(p => !onlyPublic || p.IsPublic)
            .Include(p => p.Author)
            .Include(p => p.PostCategories)
            .ToListAsync();

        return posts.Select(p => new PostModel(p)).ToList();
    }

    /// <inheritdoc/>
    public async Task<PostModel?> GetPostByIdAsync(int postId)
    {
        logger.LogInformation("Retrieving post with ID: {PostId}", postId);
        var post = await context.Posts
            .Include(p => p.Author)
            .Include(p => p.PostCategories)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == postId);

        return post == null ? null : new PostModel(post);
    }

    /// <inheritdoc/>
    public async Task<List<PostModel>> GetPostsAsync(int perPage, int pageNumber)
    {
        logger.LogInformation("Retrieving page {PageNumber} with {PerPage} posts per page", pageNumber, perPage);

        var posts = await context.Posts
            .Where(p => p.IsPublic)
            .OrderByDescending(p => p.Published)
            .Skip((pageNumber - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return posts.Select(p => new PostModel(p)).ToList();
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

        IQueryable<Post> query = context.Posts.Where(p => p.IsPublic);

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
            .ToListAsync();

        return new PaginatedPostResult
        {
            Posts = posts.Select(p => new PostModel(p)).ToList(),
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }


    /// <summary>
    /// Allows a logged-in user to "like" a post specified by ID.
    /// </summary>
    /// <param name="postId">ID of the post to like.</param>
    /// <returns>True if the post was successfully liked, otherwise false.</returns>
    public async Task<bool> LikePostAsync(int postId)
    {
        try
        {
            var userPrincipal = httpContextAccessor.HttpContext?.User;
            if (userPrincipal is null || !userPrincipal.Identity?.IsAuthenticated == true)
            {
                logger.LogWarning("No authenticated user found in HttpContext. Aborting like operation.");
                return false;
            }

            // Get the user from the identity system
            var user = await userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                logger.LogWarning("UserManager could not find a valid user. Aborting like operation.");
                return false;
            }

            logger.LogInformation("User {UserId} attempting to like post with ID: {PostId}", user.Id, postId);

            var post = await context.Posts.FindAsync(postId);
            if (post == null)
            {
                logger.LogWarning("Post with ID {PostId} not found.", postId);
                return false;
            }

            // Check if user already liked the post
            var existingLike = await context.PostLikes
                .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == user.Id);

            if (existingLike != null)
            {
                logger.LogInformation("User {UserId} has already liked post {PostId}.", user.Id, postId);
                return false;
            }

            var newLike = new PostLike
            {
                PostId = postId,
                UserId = user.Id
            };

            context.PostLikes.Add(newLike);
            await context.SaveChangesAsync();

            logger.LogInformation("User {UserId} successfully liked post {PostId}.", user.Id, postId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while liking post {PostId}.", postId);
            return false;
        }
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
    public async Task<List<PostModel>> MostPopularPostsAsync(int count)
    {
        logger.LogInformation("Retrieving {Count} most popular posts", count);
        var posts = await context.Posts
            .Include(p => p.Author)
            .Where(p => p.IsPublic)
            .OrderByDescending(p => p.PostViews)
            .Take(count)
            .ToListAsync();

        return posts.Select(p => new PostModel(p)).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<PostModel>> MostRecentPostsAsync(int count)
    {
        logger.LogInformation("Retrieving {Count} most recent posts", count);
        var posts = await context.Posts
            .Where(p => p.IsPublic)
            .OrderByDescending(p => p.Published)
            .Take(count)
            .ToListAsync();

        return posts.Select(p => new PostModel(p)).ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> UpdatePostAsync(PostModel postModel, ClaimsPrincipal userPrincipal)
    {
        var user = await userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            logger.LogWarning("No logged-in user found. Aborting post update.");
            return false;
        }

        logger.LogInformation("Updating post with ID: {PostId}", postModel.Id);
        var existingPost = await context.Posts.FindAsync(postModel.Id);
        if (existingPost == null)
            return false;

        existingPost.Title = postModel.Title;
        existingPost.Content = postModel.Content;
        existingPost.Description = postModel.Description;
        existingPost.Cover = postModel.Cover;
        existingPost.IsFeatured = postModel.IsFeatured;
        existingPost.IsPublic = postModel.IsPublic;
        existingPost.PostType = postModel.PostType;
        existingPost.PostViews = postModel.PostViews;
        existingPost.Published = postModel.Published;
        existingPost.Rating = postModel.Rating;
        existingPost.Selected = postModel.Selected;
        existingPost.Slug = postModel.Slug;
        existingPost.ModifiedDate = DateTime.UtcNow;
        existingPost.ModifiedID = user.Id;

        context.Posts.Update(existingPost);
        await context.SaveChangesAsync();
        return true;
    }
}
