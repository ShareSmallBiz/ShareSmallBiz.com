namespace ShareSmallBiz.Portal.Data.Entities;

public class SocialLink : BaseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Platform { get; set; } = string.Empty;

    [Required]
    public string Url { get; set; } = string.Empty;

    public virtual ShareSmallBizUser? User { get; set; } = null!;
}

