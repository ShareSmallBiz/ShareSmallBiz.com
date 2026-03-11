using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Tests;

[TestClass]
public class ArticleServiceTests
{
    private static ArticleService CreateService(ShareSmallBiz.Portal.Data.ShareSmallBizUserContext ctx)
        => new(ctx, TestHelper.CreateLogger<ArticleService>(), TestHelper.CreateCache());

    private static async Task<(ShareSmallBiz.Portal.Data.ShareSmallBizUserContext ctx, ShareSmallBiz.Portal.Data.Entities.ShareSmallBizUser author)> SeedArticlesAsync()
    {
        var context = TestHelper.CreateContext();
        var author = TestHelper.CreateUser();
        context.Users.Add(author);
        context.Posts.AddRange(
            TestHelper.CreatePost(author, isPublic: true,  isFeatured: true,  category: "Marketing", title: "SEO Basics"),
            TestHelper.CreatePost(author, isPublic: true,  isFeatured: false, category: "Marketing", title: "Email Marketing Guide"),
            TestHelper.CreatePost(author, isPublic: true,  isFeatured: false, category: "Finance",   title: "Budgeting for Small Biz"),
            TestHelper.CreatePost(author, isPublic: false, isFeatured: false, category: "Marketing", title: "Private Draft")
        );
        await context.SaveChangesAsync();
        return (context, author);
    }

    [TestMethod]
    public async Task GetArticlesAsync_ReturnsOnlyPublicPosts()
    {
        var (context, _) = await SeedArticlesAsync();
        var service = CreateService(context);

        var result = await service.GetArticlesAsync();

        Assert.AreEqual(3, result.TotalCount);
        Assert.IsTrue(result.Articles.All(a => a.Content is null), "List view must omit content");
    }

    [TestMethod]
    public async Task GetArticlesAsync_FilterByCategory_ReturnsMatchingOnly()
    {
        var (context, _) = await SeedArticlesAsync();
        var service = CreateService(context);

        var result = await service.GetArticlesAsync(category: "Marketing");

        Assert.AreEqual(2, result.TotalCount); // SEO Basics + Email Marketing Guide (private excluded)
        Assert.IsTrue(result.Articles.All(a => a.Category == "Marketing"));
    }

    [TestMethod]
    public async Task GetArticlesAsync_Pagination_ReturnsCorrectPage()
    {
        var (context, _) = await SeedArticlesAsync();
        var service = CreateService(context);

        var page1 = await service.GetArticlesAsync(pageNumber: 1, pageSize: 2);
        var page2 = await service.GetArticlesAsync(pageNumber: 2, pageSize: 2);

        Assert.AreEqual(2, page1.Articles.Count);
        Assert.AreEqual(1, page2.Articles.Count);
        Assert.AreEqual(2, page1.TotalPages);
    }

    [TestMethod]
    public async Task GetArticlesAsync_FilterFeatured_ReturnsOnlyFeatured()
    {
        var (context, _) = await SeedArticlesAsync();
        var service = CreateService(context);

        var result = await service.GetArticlesAsync(featured: true);

        Assert.AreEqual(1, result.TotalCount);
        Assert.IsTrue(result.Articles[0].IsFeatured);
    }

    [TestMethod]
    public async Task GetFeaturedAsync_ReturnsOnlyFeaturedArticles()
    {
        var (context, _) = await SeedArticlesAsync();
        var service = CreateService(context);

        var featured = await service.GetFeaturedAsync(count: 5);

        Assert.AreEqual(1, featured.Count);
        Assert.IsTrue(featured.All(a => a.IsFeatured));
    }

    [TestMethod]
    public async Task GetFeaturedAsync_RespectsCountLimit()
    {
        var context = TestHelper.CreateContext();
        var author = TestHelper.CreateUser();
        context.Users.Add(author);
        for (var i = 0; i < 10; i++)
            context.Posts.Add(TestHelper.CreatePost(author, isPublic: true, isFeatured: true, title: $"Featured {i}"));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var featured = await service.GetFeaturedAsync(count: 3);

        Assert.AreEqual(3, featured.Count);
    }

    [TestMethod]
    public async Task GetCategoriesAsync_ReturnsDistinctCategoriesWithCounts()
    {
        var (context, _) = await SeedArticlesAsync();
        var service = CreateService(context);

        var categories = await service.GetCategoriesAsync();

        Assert.AreEqual(2, categories.Count); // Marketing and Finance (private post's category excluded)
        var marketing = categories.FirstOrDefault(c => c.Name == "Marketing");
        Assert.IsNotNull(marketing);
        Assert.AreEqual(2, marketing.ArticleCount);
    }

    [TestMethod]
    public async Task GetCategoriesAsync_CachesResult()
    {
        var (context, author) = await SeedArticlesAsync();
        var cache = TestHelper.CreateCache();
        var service = new ArticleService(context, TestHelper.CreateLogger<ArticleService>(), cache);

        var first = await service.GetCategoriesAsync();

        // Add a new post with a new category
        context.Posts.Add(TestHelper.CreatePost(author, isPublic: true, category: "NewCategory"));
        await context.SaveChangesAsync();

        var second = await service.GetCategoriesAsync(); // should be cached

        Assert.AreEqual(first.Count, second.Count);
    }

    [TestMethod]
    public async Task GetBySlugAsync_ReturnsArticleWithContent()
    {
        var (context, _) = await SeedArticlesAsync();
        var service = CreateService(context);

        // Get the slug of a known article
        var slug = context.Posts.First(p => p.IsPublic).Slug;

        var article = await service.GetBySlugAsync(slug);

        Assert.IsNotNull(article);
        Assert.IsNotNull(article.Content, "Detail view must include content");
    }

    [TestMethod]
    public async Task GetBySlugAsync_NonExistentSlug_ReturnsNull()
    {
        var (context, _) = await SeedArticlesAsync();
        var service = CreateService(context);

        var result = await service.GetBySlugAsync("this-does-not-exist");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetRelatedAsync_ReturnsSameCategoryArticles()
    {
        var (context, _) = await SeedArticlesAsync();
        var service = CreateService(context);

        var slug = context.Posts.First(p => p.IsPublic && p.Category == "Marketing").Slug;

        var related = await service.GetRelatedAsync(slug, count: 5);

        Assert.IsTrue(related.Count >= 1);
        Assert.IsTrue(related.All(a => a.Slug != slug), "Source article must not appear in related");
    }
}
