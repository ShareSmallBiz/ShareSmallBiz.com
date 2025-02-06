using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Infrastructure.Services
{
    public class UserModel
    {
        public string Id { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Bio { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;

        // User statistics
        public int PostCount { get; set; } = 0;
        public int LikeCount { get; set; } = 0;

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
            UserName = author.UserName;
            DisplayName = author.DisplayName;
            FirstName = author.FirstName;
            LastName = author.LastName;
            Bio = author.Bio;
            ProfilePictureUrl = author.ProfilePictureUrl;

            PostCount = author.Posts?.Count ?? 0;
            LikeCount = author.LikedPosts?.Count ?? 0;
        }
    }
}
