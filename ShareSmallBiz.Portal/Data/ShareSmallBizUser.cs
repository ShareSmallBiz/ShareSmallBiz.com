using Microsoft.CodeAnalysis.Elfie.Model;

namespace ShareSmallBiz.Portal.Data;

public class ShareSmallBizUser : IdentityUser, IUserConfirmation<ShareSmallBizUser>
{
    public ShareSmallBizUser(ShareSmallBizUser user)
    {
        Id = user.Id;
        UserName = user.UserName;
        NormalizedUserName = user.NormalizedUserName;
        Email = user.Email;
        NormalizedEmail = user.NormalizedEmail;
        EmailConfirmed = user.EmailConfirmed;
        PasswordHash = user.PasswordHash;
        SecurityStamp = user.SecurityStamp;
        ConcurrencyStamp = user.ConcurrencyStamp;
        PhoneNumber = user.PhoneNumber;
        PhoneNumberConfirmed = user.PhoneNumberConfirmed;
        TwoFactorEnabled = user.TwoFactorEnabled;
        LockoutEnd = user.LockoutEnd;
        LockoutEnabled = user.LockoutEnabled;
        AccessFailedCount = user.AccessFailedCount;
        Followers = [];
        Following = [];
        LikedPosts = [];
        LikedPostComments = [];
        Posts = [];
        SocialLinks = [];
    }
    public ShareSmallBizUser()
    {
        Followers = [];
        Following = [];
        LikedPosts = [];
        LikedPostComments = [];
        Posts = [];
        SocialLinks = [];
    }

    // ---- BASIC USER INFO ----
    public string DisplayName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; } = string.Empty;

    public byte[]? ProfilePicture { get; set; }
    public string ProfilePictureUrl { get; set; } = string.Empty;

    // ---- SEO & PUBLIC PROFILE ----
    public string? Slug { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? MetaDescription { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Keywords { get; set; } = string.Empty;

    // ---- SOCIAL & SERVICES ----
    public virtual ICollection<SocialLink> SocialLinks { get; set; }

    // ---- SOCIAL INTERACTIONS ----
    public ICollection<UserFollow> Followers { get; set; }
    public ICollection<UserFollow> Following { get; set; }
    public virtual ICollection<PostLike> LikedPosts { get; set; }
    public virtual ICollection<PostCommentLike> LikedPostComments { get; set; }
    public virtual ICollection<Post> Posts { get; set; }
    public string? WebsiteUrl { get; set; }
    public DateTime LastModified { get; set; } = DateTime.Now;
    public ICollection<Post> ReceivedPosts { get; set; } = [];

    public Task<bool> IsConfirmedAsync(UserManager<ShareSmallBizUser> manager, ShareSmallBizUser user)
    {
        // Implement the method to check if the user is confirmed
        return Task.FromResult(user.EmailConfirmed);
    }
}

public class SocialLink
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Platform { get; set; } = string.Empty;

    [Required]
    public string Url { get; set; } = string.Empty;

    public string? UserId { get; set; }
    public virtual ShareSmallBizUser? User { get; set; } = null!;
}

