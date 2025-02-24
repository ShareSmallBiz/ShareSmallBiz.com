using Microsoft.AspNetCore.Identity;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareSmallBiz.Portal.Data;


public class ShareSmallBizUser : IdentityUser
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
        Testimonials = [];
        Services = [];
        Collaborations = [];
        ContentContributions = [];
    }
    public ShareSmallBizUser()
    {
        Followers = [];
        Following = [];
        LikedPosts = [];
        LikedPostComments = [];
        Posts = [];
        SocialLinks = [];
        Testimonials = [];
        Services = [];
        Collaborations = [];
        ContentContributions = [];
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

    // ---- BUSINESS RELATIONSHIPS ----
    public bool IsSoleProprietor { get; set; } = false;

    // Business Ownership (One-to-One)
    public string? OwnedBusinessId { get; set; }
    [ForeignKey("OwnedBusinessId")]
    public virtual BusinessProfile? OwnedBusiness { get; set; }

    // Employment in a Business (One-to-Many)
    public string? BusinessId { get; set; }
    [ForeignKey("BusinessId")]
    public virtual BusinessProfile? Business { get; set; }

    // ---- SOCIAL & SERVICES ----
    public virtual ICollection<UserService> Services { get; set; }
    public virtual ICollection<SocialLink> SocialLinks { get; set; }
    public virtual ICollection<Testimonial> Testimonials { get; set; }
    public virtual ICollection<UserCollaboration> Collaborations { get; set; }
    public virtual ICollection<UserContentContribution> ContentContributions { get; set; }

    // ---- SOCIAL INTERACTIONS ----
    public ICollection<UserFollow> Followers { get; set; }
    public ICollection<UserFollow> Following { get; set; }
    public virtual ICollection<PostLike> LikedPosts { get; set; }
    public virtual ICollection<PostCommentLike> LikedPostComments { get; set; }
    public virtual ICollection<Post> Posts { get; set; }
    public string? WebsiteUrl { get; set; }
}

public class BusinessProfile
{
    public BusinessProfile()
    {
        Employees = [];
        Services = [];
        Testimonials = [];
        SocialLinks = [];
        Collaborations = [];
        ContentContributions = [];
    }

    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string BusinessName { get; set; } = string.Empty;

    [Required]
    public string Industry { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string BusinessDescription { get; set; } = string.Empty;

    // ---- SEO & PUBLIC VISIBILITY ----
    public string? Slug { get; set; } = string.Empty;

    [MaxLength(160)]
    public string MetaDescription { get; set; } = string.Empty;

    [MaxLength(250)]
    public string Keywords { get; set; } = string.Empty;

    // ---- CONTACT & SOCIAL ----
    public string WebsiteUrl { get; set; } = string.Empty;
    public string BusinessEmail { get; set; } = string.Empty;
    public string BusinessPhone { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public virtual ICollection<SocialLink> SocialLinks { get; set; }

    // ---- BUSINESS RELATIONSHIPS ----

    // Business Owner (One-to-One)
    public string OwnerId { get; set; } = string.Empty;
    [ForeignKey("OwnerId")]
    public virtual ShareSmallBizUser Owner { get; set; } = null!;

    // Employees (One-to-Many)
    public virtual ICollection<ShareSmallBizUser> Employees { get; set; }

    public virtual ICollection<UserService> Services { get; set; }
    public virtual ICollection<Testimonial> Testimonials { get; set; }
    public virtual ICollection<UserCollaboration> Collaborations { get; set; }
    public virtual ICollection<UserContentContribution> ContentContributions { get; set; }
}


public class UserService
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    public bool IsBusinessService { get; set; } = false; // If true, service is for a business
    public string? UserId { get; set; }
    public virtual ShareSmallBizUser? User { get; set; } = null!;
    public string? BusinessId { get; set; }
    public virtual BusinessProfile? Business { get; set; } = null!;
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

    public string? BusinessId { get; set; }
    public virtual BusinessProfile? Business { get; set; } = null!;
}
public class Testimonial
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public string ReviewerName { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    public string? UserId { get; set; }
    public virtual ShareSmallBizUser? User { get; set; } = null!;

    public string? BusinessId { get; set; }
    public virtual BusinessProfile? Business { get; set; } = null!;
}

/// <summary>
/// Represents a collaboration between two users or businesses on ShareSmallBiz.com.
/// </summary>
public class UserCollaboration
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string CollaborationTitle { get; set; } = string.Empty; // e.g., "Marketing Partnership"

    [MaxLength(1000)]
    public string CollaborationDetails { get; set; } = string.Empty; // Description of the partnership

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; } // Nullable if ongoing

    // ---- RELATIONSHIPS ----

    // Collaboration Between Users
    public string? UserId1 { get; set; }
    [ForeignKey("UserId1")]
    public virtual ShareSmallBizUser? User1 { get; set; } = null!;

    public string? UserId2 { get; set; }
    [ForeignKey("UserId2")]
    public virtual ShareSmallBizUser? User2 { get; set; } = null!;

    // Collaboration Between Businesses
    public string? BusinessId1 { get; set; }
    [ForeignKey("BusinessId1")]
    public virtual BusinessProfile? Business1 { get; set; } = null!;

    public string? BusinessId2 { get; set; }
    [ForeignKey("BusinessId2")]
    public virtual BusinessProfile? Business2 { get; set; } = null!;
}

/// <summary>
/// Represents user-generated content contributions like blogs, podcasts, or Q&A responses.
/// </summary>
public class UserContentContribution
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string ContentUrl { get; set; } = string.Empty; // Link to the blog, podcast, or Q&A

    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty; // Short description for SEO & previews

    public string ContentType { get; set; } = "Blog"; // e.g., "Blog", "Podcast", "Q&A"

    public DateTime DatePublished { get; set; } = DateTime.UtcNow;

    // ---- RELATIONSHIPS ----

    public string? UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual ShareSmallBizUser? User { get; set; } = null!;

    public string? BusinessId { get; set; }
    [ForeignKey("BusinessId")]
    public virtual BusinessProfile? Business { get; set; } = null!;
}
