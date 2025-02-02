namespace ShareSmallBiz.Portal.Data;
/// <summary>
/// Add profile data for application users by adding properties to the ShareSmallBizUser class
/// </summary>
public class ShareSmallBizUser : IdentityUser
{
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

    // Liked Posts
    public ICollection<PostLike> LikedPosts { get; set; } = [];
    /// <summary>
    /// Profile Picture of User
    /// </summary>
    public byte[]? ProfilePicture { get; set; }
    public string ProfilePictureUrl { get; set; } = string.Empty;
}

