// First, let's create the Media entity class
using ShareSmallBiz.Portal.Data.Enums;
using System;

namespace ShareSmallBiz.Portal.Data.Entities;

public class Media : BaseEntity
{
    // Id inherited from BaseEntity. Removed duplicate to avoid CS0108 warning.

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public MediaType MediaType { get; set; }

    [Required]
    public StorageProviderNames StorageProvider { get; set; }

    [Required]
    [StringLength(2048)]
    public string Url { get; set; } = string.Empty;  // URL or file path depending on storage type

    [StringLength(255)]
    public string? ContentType { get; set; }  // MIME type (may be inferred later)

    public long? FileSize { get; set; }  // in bytes

    [StringLength(512)]
    public string? Description { get; set; }

    [StringLength(1024)]
    public string? StorageMetadata { get; set; }  // JSON string for additional storage-specific metadata

    // Attribution for external content
    [StringLength(255)]
    public string? Attribution { get; set; }

    // Relationship with the user who uploaded or linked the media
    public string UserId { get; set; } = string.Empty;
    public virtual ShareSmallBizUser? User { get; set; }

    // Optional parent Post relationship
    public int? PostId { get; set; }
    public virtual Post? Post { get; set; }

    // Optional parent Comment relationship
    public int? CommentId { get; set; }
    public virtual PostComment? Comment { get; set; }
}

