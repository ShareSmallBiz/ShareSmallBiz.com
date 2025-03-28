using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class ProfileModel : UserModel
{
    public ProfileModel()
    {

    }
    public ProfileModel(ShareSmallBizUser author) : base(author)
    {
    }
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
        Bio = author.Bio;
        ProfilePictureUrl = author.ProfilePictureUrl;
        ProfilePicture = author.ProfilePicture;
        PostCount = author.Posts?.Count ?? 0;
        LikeCount = author.LikeCount;
        Posts = author?.Posts ?? [];

    }
    public List<UserModel> PublicUsers { get; set; } = [];
}


public class UserModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Bio { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public byte[]? ProfilePicture { get; set; }
    public int PostCount { get; set; } = 0;
    public List<DiscussionModel> Posts { get; set; } = [];
    public int LikeCount { get; set; } = 0;
    public IEnumerable<string> Roles { get; set; } = [];
    public bool IsLockedOut { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public UserModel() { }
    public UserModel(string id, string username, int postCount, int likeCount)
    {
        Id = id;
        UserName = username;
        PostCount = postCount;
        LikeCount = likeCount;
    }
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
        Bio = author.Bio;
        ProfilePictureUrl = author.ProfilePictureUrl;
        ProfilePicture = author.ProfilePicture;
        PostCount = author.Posts?.Count ?? 0;
        LikeCount = author.LikedPosts?.Count ?? 0;

        if (author.Posts != null)
        {
            Posts = GetPostList(author);
        }
    }

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
                Author = new UserModel()
                {
                    Id = post.Author.Id,
                    Email = post.Author.Email,
                    UserName = post.Author.UserName,
                    DisplayName = post.Author.DisplayName,
                    FirstName = post.Author.FirstName,
                    LastName = post.Author.LastName,
                    Bio = post.Author.Bio,
                    ProfilePictureUrl = post.Author.ProfilePictureUrl,
                    ProfilePicture = post.Author.ProfilePicture
                }
            }
            );
        }
        return Posts;
    }

}
