using Microsoft.EntityFrameworkCore;
using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class PostProvider(ShareSmallBizUserContext context, ILogger<PostProvider> logger)
: IPostProvider
{
    // Create a new post
    public async Task<PostModel> CreatePostAsync(PostModel postModel)
    {
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
            CreatedID = postModel.CreatedID,
            ModifiedID = postModel.ModifiedID
        };

        context.Posts.Add(post);
        await context.SaveChangesAsync();

        return new PostModel(post);
    }

    // Retrieve a post by ID
    public async Task<PostModel?> GetPostByIdAsync(int postId)
    {
        logger.LogInformation("Retrieving post with ID: {PostId}", postId);
        var post = await context.Posts.Include(p => p.Author)
                                       .Include(p => p.PostCategories)
                                       .FirstOrDefaultAsync(p => p.Id == postId);
        return post == null ? null : new PostModel(post);
    }

    // Retrieve all posts with optional filters
    public async Task<List<PostModel>> GetAllPostsAsync(bool onlyPublic = true)
    {
        logger.LogInformation("Retrieving all posts. Only public: {OnlyPublic}", onlyPublic);

        var posts = await context.Posts.Where(p => !onlyPublic || p.IsPublic)
                                        .Include(p => p.Author)
                                        .Include(p => p.PostCategories)
                                        .ToListAsync();
        return posts.Select(p => new PostModel(p)).ToList();
    }

    // Update an existing post
    public async Task<bool> UpdatePostAsync(PostModel postModel)
    {
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
        existingPost.ModifiedID = postModel.ModifiedID;

        context.Posts.Update(existingPost);
        await context.SaveChangesAsync();
        return true;
    }

    // Delete a post
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
}

public class PostModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Cover { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsPublic { get; set; }
    public Models.PostType PostType { get; set; }
    public int PostViews { get; set; }
    public DateTime Published { get; set; }
    public double Rating { get; set; }
    public bool Selected { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int? CreatedID { get; set; }
    public int? ModifiedID { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public PostModel() { }

    public PostModel(Post post)
    {
        Id = post.Id;
        Title = post.Title;
        Content = post.Content;
        Description = post.Description;
        Cover = post.Cover;
        IsFeatured = post.IsFeatured;
        IsPublic = post.IsPublic;
        PostType = post.PostType;
        PostViews = post.PostViews;
        Published = post.Published;
        Rating = post.Rating;
        Selected = post.Selected;
        Slug = post.Slug;
        CreatedID = post.CreatedID;
        ModifiedID = post.ModifiedID;
        CreatedDate = post.CreatedDate;
        ModifiedDate = post.ModifiedDate;
    }
}

