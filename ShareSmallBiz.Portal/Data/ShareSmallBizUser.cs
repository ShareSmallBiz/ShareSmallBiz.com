namespace ShareSmallBiz.Portal.Data;
/// <summary>
/// Add profile data for application users by adding properties to the ShareSmallBizUser class
/// </summary>
public class ShareSmallBizUser : IdentityUser
{
    public ShareSmallBizUser()
    {
            
    }
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
    }

    public string Bio { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// First Name of User
    /// </summary>
    public string? FirstName { get; set; }

    // Followers and Following
    public ICollection<UserFollow> Followers { get; set; } = [];
    public ICollection<UserFollow> Following { get; set; } = [];
    /// <summary>
    /// Last Name of User
    /// </summary>
    public string? LastName { get; set; }
    public byte[]? ProfilePicture { get; set; }
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public ICollection<PostLike> LikedPosts { get; set; } = [];
    public virtual ICollection<PostCommentLike> LikedPostComments { get; set; } = [];
    public virtual ICollection<Post> Posts { get; set; } = [];
}

