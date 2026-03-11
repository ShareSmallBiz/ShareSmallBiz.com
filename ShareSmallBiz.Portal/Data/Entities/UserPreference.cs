using ShareSmallBiz.Portal.Data.Enums;

namespace ShareSmallBiz.Portal.Data.Entities;

public class UserPreference : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public virtual ShareSmallBizUser User { get; set; } = null!;

    // ---- NOTIFICATION PREFERENCES ----
    public bool EmailOnComment { get; set; } = true;
    public bool EmailOnLike { get; set; } = false;
    public bool EmailOnFollow { get; set; } = true;
    public bool WeeklySummary { get; set; } = false;

    // ---- PRIVACY PREFERENCES ----
    public ProfileVisibility ProfileVisibility { get; set; } = ProfileVisibility.Public;
    public bool ShowEmail { get; set; } = false;
    public bool ShowWebsite { get; set; } = true;
}
