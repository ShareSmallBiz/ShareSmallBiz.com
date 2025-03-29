namespace ShareSmallBiz.Portal.Infrastructure.Models;

public class KeywordModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int PostCount { get; set; }
}
