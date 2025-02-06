using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class PostSeed(ShareSmallBizUserContext context, ILogger<PostSeed> logger)
{
    public async Task SeedPostsAsync()
    {
        logger.LogInformation("Seeding initial posts for ShareSmallBiz Forum...");

        if (await context.Posts.AnyAsync())
        {
            logger.LogInformation("Posts already exist. Skipping seed.");
            return;
        }
        var admin = "0819728c-5762-4ae3-9e82-7a3193441c76";
        var seedPosts = new List<Post>
{
    new Post
    {
        Title = "Welcome to ShareSmallBiz Community!",
        Content = "Introduce yourself and tell us about your business. Let's grow together!",
        Description = "A friendly welcome message to new members.",
        Cover = "https://sharesmallbiz.com/images/welcome.jpg",
        IsFeatured = true,
        IsPublic = true,
        PostType = Models.PostType.Post,
        PostViews = 120,
        Published = DateTime.UtcNow.AddDays(-1),
        Rating = 5,
        Selected = true,
        Slug = "welcome-to-share-smallbiz",
        CreatedDate = DateTime.UtcNow.AddDays(-1),
        ModifiedDate = DateTime.UtcNow.AddDays(-1),
        CreatedID = admin,
        ModifiedID = admin,
        AuthorId = admin
    },
    new Post
    {
        Title = "Best Marketing Strategies for Small Businesses",
        Content = "What marketing strategies have worked best for your business?",
        Description = "Share your experiences and learn new marketing tactics!",
        Cover = "https://sharesmallbiz.com/images/marketing.jpg",
        IsFeatured = false,
        IsPublic = true,
        PostType = Models.PostType.Post,
        PostViews = 85,
        Published = DateTime.UtcNow.AddDays(-2),
        Rating = 4.5,
        Selected = false,
        Slug = "best-marketing-strategies",
        CreatedDate = DateTime.UtcNow.AddDays(-2),
        ModifiedDate = DateTime.UtcNow.AddDays(-2),
        CreatedID = admin,
        ModifiedID = admin,
        AuthorId = admin
    },
    new Post
    {
        Title = "How to Generate More Leads Without Paid Ads",
        Content = "Have you tried organic lead generation strategies? Let’s discuss!",
        Description = "A discussion about alternative lead generation methods.",
        Cover = "https://sharesmallbiz.com/images/leads.jpg",
        IsFeatured = false,
        IsPublic = true,
        PostType = Models.PostType.Post,
        PostViews = 65,
        Published = DateTime.UtcNow.AddDays(-3),
        Rating = 4.2,
        Selected = false,
        Slug = "generate-more-leads",
        CreatedDate = DateTime.UtcNow.AddDays(-3),
        ModifiedDate = DateTime.UtcNow.AddDays(-3),
        CreatedID = admin,
        ModifiedID = admin,
        AuthorId = admin
    },
    new Post
    {
        Title = "Looking for Collaboration Partners",
        Content = "If you're open to collaborations, post your business details here!",
        Description = "A space for networking and finding business partners.",
        Cover = "https://sharesmallbiz.com/images/collab.jpg",
        IsFeatured = true,
        IsPublic = true,
        PostType = Models.PostType.Post,
        PostViews = 100,
        Published = DateTime.UtcNow.AddDays(-4),
        Rating = 4.7,
        Selected = true,
        Slug = "looking-for-collaborations",
        CreatedDate = DateTime.UtcNow.AddDays(-4),
        ModifiedDate = DateTime.UtcNow.AddDays(-4),
        CreatedID = admin,
        ModifiedID = admin,
        AuthorId = admin
    },
    new Post
    {
        Title = "Tools & Software Every Small Business Should Use",
        Content = "What tools do you use to manage and scale your business?",
        Description = "Share your favorite productivity, marketing, and finance tools.",
        Cover = "https://sharesmallbiz.com/images/tools.jpg",
        IsFeatured = false,
        IsPublic = true,
        PostType = Models.PostType.Post,
        PostViews = 90,
        Published = DateTime.UtcNow.AddDays(-5),
        Rating = 4.6,
        Selected = false,
        Slug = "best-tools-for-smallbiz",
        CreatedDate = DateTime.UtcNow.AddDays(-5),
        ModifiedDate = DateTime.UtcNow.AddDays(-5),
        CreatedID = admin,
        ModifiedID = admin,
        AuthorId = admin
    },
    new Post
    {
        Title = "Share Your Business Success Story",
        Content = "Inspire others by sharing your journey as a small business owner.",
        Description = "Tell us about your struggles, wins, and lessons learned.",
        Cover = "https://sharesmallbiz.com/images/success.jpg",
        IsFeatured = true,
        IsPublic = true,
        PostType = Models.PostType.Post,
        PostViews = 150,
        Published = DateTime.UtcNow.AddDays(-6),
        Rating = 5,
        Selected = true,
        Slug = "share-your-success-story",
        CreatedDate = DateTime.UtcNow.AddDays(-6),
        ModifiedDate = DateTime.UtcNow.AddDays(-6),
        CreatedID = admin,
        ModifiedID = admin,
        AuthorId = admin
    },
    new Post
    {
        Title = "Affiliate Marketing for Small Businesses",
        Content = "How can small businesses leverage affiliate marketing?",
        Description = "Discuss strategies and platforms for affiliate marketing success.",
        Cover = "https://sharesmallbiz.com/images/affiliate.jpg",
        IsFeatured = false,
        IsPublic = true,
        PostType = Models.PostType.Post,
        PostViews = 70,
        Published = DateTime.UtcNow.AddDays(-7),
        Rating = 4.4,
        Selected = false,
        Slug = "affiliate-marketing-tips",
        CreatedDate = DateTime.UtcNow.AddDays(-7),
        ModifiedDate = DateTime.UtcNow.AddDays(-7),
        CreatedID = admin,
        ModifiedID = admin,
        AuthorId = admin
    }
};
        await context.Posts.AddRangeAsync(seedPosts);
        await context.SaveChangesAsync();
        logger.LogInformation("Successfully seeded {Count} posts.", seedPosts.Count);
    }

}

