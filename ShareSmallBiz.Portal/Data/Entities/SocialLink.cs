namespace ShareSmallBiz.Portal.Data.Entities;

public class SocialLink : BaseEntity
{
    [Required]
    public string Platform { get; set; } = string.Empty;

    [Required]
    public string Url { get; set; } = string.Empty;

}

