using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace ShareSmallBiz.Portal.Infrastructure.Models;

/// <summary>
/// Represents a user profile model, extending the base UserModel.
/// </summary>
public class ProfileModel : UserModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileModel"/> class.
    /// </summary>
    public ProfileModel()
    {

    }
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileModel"/> class based on a ShareSmallBizUser entity.
    /// </summary>
    /// <param name="author">The user entity.</param>
    public ProfileModel(ShareSmallBizUser author) : base(author)
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileModel"/> class based on a UserModel.
    /// </summary>
    /// <param name="author">The user model.</param>
    public ProfileModel(UserModel author)
    {
        if (author == null)
            return;

        Id = author.Id;
        Email = author.Email ?? string.Empty;
        UserName = author.UserName ?? string.Empty;
        DisplayName = author.DisplayName;
        WebsiteUrl = author.WebsiteUrl ?? string.Empty;
        FirstName = author.FirstName;
        LastName = author.LastName;
        Bio = author.Bio ?? string.Empty; // Handle potential null
        ProfilePictureUrl = author.ProfilePictureUrl ?? string.Empty; // Handle potential null
        PostCount = author.Posts?.Count ?? 0;
        LikeCount = author.LikeCount;
        Posts = author?.Posts ?? [];

    }
    /// <summary>
    /// Gets or sets the list of public users.
    /// </summary>
    public List<UserModel> PublicUsers { get; set; } = [];

    /// <summary>
    /// Gets or sets profile analytics data
    /// </summary>
    public ProfileAnalytics Analytics { get; set; } = new();
}

/// <summary>
/// Represents profile analytics data
/// </summary>
public class ProfileAnalytics
{
    /// <summary>
    /// Gets or sets total profile views
    /// </summary>
    public int TotalViews { get; set; }
    
    /// <summary>
    /// Gets or sets profile view data for the last 30 days
    /// </summary>
    public Dictionary<DateTime, int> RecentViews { get; set; } = new();
    
    /// <summary>
    /// Gets or sets geographic distribution of viewers
    /// </summary>
    public Dictionary<string, int> GeoDistribution { get; set; } = new();
    
    /// <summary>
    /// Gets or sets engagement metrics for the profile
    /// </summary>
    public EngagementMetrics Engagement { get; set; } = new();
}

/// <summary>
/// Represents engagement metrics for a user profile
/// </summary>
public class EngagementMetrics
{
    /// <summary>
    /// Gets or sets total follower count
    /// </summary>
    public int FollowerCount { get; set; }
    
    /// <summary>
    /// Gets or sets new follower count in the last 30 days
    /// </summary>
    public int NewFollowers { get; set; }
    
    /// <summary>
    /// Gets or sets total likes received on all content
    /// </summary>
    public int TotalLikes { get; set; }
    
    /// <summary>
    /// Gets or sets likes received in the last 30 days
    /// </summary>
    public int RecentLikes { get; set; }
}

/// <summary>
/// Represents a base user model.
/// </summary>
public class UserModel
{
    /// <summary>
    /// Gets or sets the user's unique identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the user's username.
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the user's website URL.
    /// </summary>
    public string WebsiteUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string? FirstName { get; set; }
    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string? LastName { get; set; }
    /// <summary>
    /// Gets or sets the user's biography.
    /// </summary>
    public string Bio { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the URL of the user's profile picture.
    /// </summary>
    public string ProfilePictureUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the number of posts created by the user.
    /// </summary>
    public int PostCount { get; set; } = 0;
    /// <summary>
    /// Gets or sets the list of discussions (posts) created by the user.
    /// </summary>
    public List<DiscussionModel> Posts { get; set; } = [];
    /// <summary>
    /// Gets or sets the number of likes received by the user.
    /// </summary>
    public int LikeCount { get; set; } = 0;
    /// <summary>
    /// Gets or sets the roles assigned to the user.
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether the user is locked out.
    /// </summary>
    public bool IsLockedOut { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the user's email is confirmed.
    /// </summary>
    public bool IsEmailConfirmed { get; set; }
    /// <summary>
    /// Gets or sets the date and time the user profile was last modified.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.Now;
    /// <summary>
    /// Gets or sets the date and time of the user's last login.
    /// </summary>
    public DateTime? LastLogin { get; set; } // Add this
    /// <summary>
    /// Gets or sets the number of times the user has logged in.
    /// </summary>
    public int LoginCount { get; set; }      // Add this
    /// <summary>
    /// Gets or sets the profile picture file uploaded by the user.
    /// </summary>
    [Display(Name = "Profile Picture")]
    public IFormFile? ProfilePictureFile { get; set; }

    /// <summary>
    /// Gets or sets the option for handling the profile picture (keep, upload, url, remove).
    /// </summary>
    [Display(Name = "Profile Picture Option")]
    public string ProfilePictureOption { get; set; } = "keep"; // keep, upload, url, remove

    /// <summary>
    /// Gets or sets a value indicating whether the user has a profile picture.
    /// </summary>
    public bool HasProfilePicture { get; set; }
    /// <summary>
    /// Gets or sets the preview URL for the profile picture.
    /// </summary>
    public string? ProfilePicturePreview { get; set; }
    /// <summary>
    /// Gets or sets the visibility level of the user profile
    /// </summary>
    public ProfileVisibility ProfileVisibility { get; set; } = ProfileVisibility.Public;
    
    /// <summary>
    /// Gets or sets the custom profile URL if different from the generated slug
    /// </summary>
    [Display(Name = "Custom Profile URL")]
    [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "Custom URL can only contain letters, numbers, hyphens, and underscores")]
    [MaxLength(50, ErrorMessage = "Custom URL cannot exceed 50 characters")]
    public string? CustomProfileUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the profile view count
    /// </summary>
    public int ProfileViewCount { get; set; }
    
    /// <summary>
    /// Gets or sets the profile completeness score (0-100)
    /// </summary>
    public int ProfileCompletenessScore { get; set; }
    
    /// <summary>
    /// Gets or sets the fields that need completion to improve profile score
    /// </summary>
    public List<string> IncompleteProfileFields { get; set; } = [];
    /// <summary>
    /// Initializes a new instance of the <see cref="UserModel"/> class.
    /// </summary>
    public UserModel() { }
    /// <summary>
    /// Initializes a new instance of the <see cref="UserModel"/> class with basic information.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="username">The user's username.</param>
    /// <param name="postCount">The user's post count.</param>
    /// <param name="likeCount">The user's like count.</param>
    public UserModel(string id, string username, int postCount, int likeCount)
    {
        Id = id;
        UserName = username;
        PostCount = postCount;
        LikeCount = likeCount;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="UserModel"/> class based on a ShareSmallBizUser entity.
    /// </summary>
    /// <param name="author">The user entity.</param>
    public UserModel(ShareSmallBizUser author)
    {
        if (author == null)
            return;

        Id = author.Id;
        Email = author.Email ?? string.Empty;
        UserName = author.UserName ?? string.Empty;
        DisplayName = author.DisplayName;
        WebsiteUrl = author.WebsiteUrl ?? string.Empty;
        FirstName = author.FirstName;
        LastName = author.LastName;
        Bio = author.Bio ?? string.Empty; // Handle potential null
        ProfilePictureUrl = author.ProfilePictureUrl ?? string.Empty; // Handle potential null
        PostCount = author.Posts?.Count ?? 0;
        LikeCount = author.LikedPosts?.Count ?? 0;
        IsLockedOut = author.LockoutEnabled && (author.LockoutEnd == null || author.LockoutEnd > DateTime.Now);
        IsEmailConfirmed = author.EmailConfirmed;
        LastModified = author.LastModified != default ? author.LastModified : DateTime.Now;
        ProfileVisibility = author.ProfileVisibility;
        CustomProfileUrl = author.CustomProfileUrl;
        ProfileViewCount = author.ProfileViewCount;
        ProfileCompletenessScore = author.ProfileCompletenessScore;
        // LoginCount and LastLogin will be populated separately

        if (string.IsNullOrEmpty(DisplayName))
        {
            DisplayName = UserName; // Fallback to username if display name is not set
        }

        if (author.Posts != null)
        {
            Posts = GetPostList(author);
        }
        
        // Calculate incomplete profile fields
        CalculateIncompleteFields();
    }

    /// <summary>
    /// Calculates which profile fields are incomplete
    /// </summary>
    private void CalculateIncompleteFields()
    {
        IncompleteProfileFields = [];
        
        if (string.IsNullOrWhiteSpace(ProfilePictureUrl))
            IncompleteProfileFields.Add("Profile Picture");
            
        if (string.IsNullOrWhiteSpace(Bio))
            IncompleteProfileFields.Add("Bio");
            
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            IncompleteProfileFields.Add("Name");
            
        if (string.IsNullOrWhiteSpace(WebsiteUrl))
            IncompleteProfileFields.Add("Website");
    }

    /// <summary>
    /// Retrieves the list of discussion models for the user's posts.
    /// </summary>
    /// <param name="author">The user entity.</param>
    /// <returns>A list of <see cref="DiscussionModel"/>.</returns>
    public List<DiscussionModel> GetPostList(ShareSmallBizUser author)
    {
        if (author.Posts == null)
            return [];
        foreach (var post in author.Posts)
        {
            Posts.Add(new DiscussionModel()
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                Description = post.Description,
                Cover = post.Cover,
                IsFeatured = post.IsFeatured,
                IsPublic = post.IsPublic,
                PostType = post.PostType,
                PostViews = post.PostViews,
                Published = post.Published,
                Rating = post.Rating,
                Selected = post.Selected,
                Slug = post.Slug,
                CreatedID = post.CreatedID,
                ModifiedID = post.ModifiedID,
                CreatedDate = post.CreatedDate,
                ModifiedDate = post.ModifiedDate,
                Comments = post.Comments?.Select(comment => new PostCommentModel(comment)).ToList() ?? new List<PostCommentModel>(),
                Tags = post.PostCategories?.Select(x => x.Name).ToList() ?? new List<string>(),
                Creator = new UserModel()
                {
                    Id = post.Author.Id,
                    Email = post.Author.Email ?? string.Empty, // Handle potential null
                    UserName = post.Author.UserName ?? string.Empty, // Handle potential null
                    DisplayName = post.Author.DisplayName,
                    FirstName = post.Author.FirstName,
                    LastName = post.Author.LastName,
                    Bio = post.Author.Bio ?? string.Empty, // Handle potential null
                    ProfilePictureUrl = post.Author.ProfilePictureUrl ?? string.Empty // Handle potential null
                }
            }
            );
        }
        return Posts;
    }

}
/// <summary>
/// Represents a user model specifically for creating business users.
/// </summary>
public class CreateBusinessUserModel : UserModel
{
    /// <summary>
    /// Gets or sets the company name.
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the business phone number.
    /// </summary>
    [Display(Name = "Business Phone")]
    public string? BusinessPhone { get; set; } = string.Empty;

}

