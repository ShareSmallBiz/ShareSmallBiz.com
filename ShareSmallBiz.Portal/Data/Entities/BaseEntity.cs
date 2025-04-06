using ShareSmallBiz.Portal.Data.Entities;
using System.ComponentModel.DataAnnotations.Schema;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public string? ModifiedID { get; set; }
    public string? CreatedID { get; set; }

    // Navigation property
    [ForeignKey("CreatedID")]
    public virtual ShareSmallBizUser? Creator { get; set; }
}