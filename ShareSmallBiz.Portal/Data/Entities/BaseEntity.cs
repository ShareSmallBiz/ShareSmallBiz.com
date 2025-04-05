namespace ShareSmallBiz.Portal.Data.Entities;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public string? ModifiedID { get; set; }
    public string? CreatedID { get; set; }
}

