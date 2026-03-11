using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Data.Enums;

namespace ShareSmallBiz.Tests;

/// <summary>Shared factory helpers for all test classes.</summary>
internal static class TestHelper
{
    /// <summary>Creates an isolated in-memory DbContext with a unique database name.</summary>
    public static ShareSmallBizUserContext CreateContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ShareSmallBizUserContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new ShareSmallBizUserContext(options);
    }

    public static ILogger<T> CreateLogger<T>() => new Mock<ILogger<T>>().Object;

    public static IMemoryCache CreateCache() => new MemoryCache(new MemoryCacheOptions());

    public static ShareSmallBizUser CreateUser(
        string? id = null,
        string? displayName = null,
        string? email = null,
        ProfileVisibility visibility = ProfileVisibility.Public)
    {
        var uid = id ?? Guid.NewGuid().ToString();
        var shortId = uid.Length >= 8 ? uid[..8] : uid;
        var addr = email ?? $"user_{shortId}@test.com";
        return new ShareSmallBizUser
        {
            Id = uid,
            DisplayName = displayName ?? $"User {shortId}",
            UserName = addr,
            Email = addr,
            EmailConfirmed = true,
            NormalizedEmail = addr.ToUpperInvariant(),
            NormalizedUserName = addr.ToUpperInvariant(),
            Slug = $"user-{shortId}",
            ProfileVisibility = visibility,
            Bio = $"Bio for {displayName ?? shortId}"
        };
    }

    /// <summary>Creates a Post with the Author navigation set (required for DiscussionModel mapping).</summary>
    public static Post CreatePost(
        ShareSmallBizUser author,
        bool isPublic = true,
        bool isFeatured = false,
        string? title = null,
        string? category = null)
    {
        var suffix = Random.Shared.Next(1000, 99999);
        var postTitle = title ?? $"Test Post {suffix}";
        return new Post
        {
            Title = postTitle,
            Content = $"Content body for {postTitle}",
            Description = $"Short description for {postTitle}",
            Slug = $"test-post-{suffix}",
            AuthorId = author.Id,
            Author = author,
            IsPublic = isPublic,
            IsFeatured = isFeatured,
            Category = category,
            Published = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            PostViews = 0,
            ShareCount = 0
        };
    }
}
