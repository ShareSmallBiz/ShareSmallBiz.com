namespace ShareSmallBiz.Portal.Data;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string? ModifiedID { get; set; }
    public string? CreatedID { get; set; }
}

