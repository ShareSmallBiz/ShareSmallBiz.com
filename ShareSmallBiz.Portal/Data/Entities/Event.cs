namespace ShareSmallBiz.Portal.Data.Entities;

public class Event : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Location { get; set; }

    public bool IsOnline { get; set; } = false;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [StringLength(500)]
    public string? RegistrationUrl { get; set; }

    /// <summary>Organizer user ID (optional)</summary>
    public string? OrganizerId { get; set; }
    public virtual ShareSmallBizUser? Organizer { get; set; }
}
