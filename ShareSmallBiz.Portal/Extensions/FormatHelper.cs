namespace ShareSmallBiz.Portal.Extensions;

/// <summary>
/// 
/// </summary>
public static class FormatHelper
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GetSafePath(string name)
    {
        return name == null
            ? string.Empty
            : $"{name.Replace("&", "-").Replace("\n", string.Empty).Replace("/", "-").Replace("'", "-").Replace(" ", "-").ToLower(CultureInfo.CurrentCulture)}";
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="root"></param>
    /// <returns></returns>
    public static string GetSafePath(string name, string root)
    {
        return $"{GetSafePath(root)}{GetSafePath(name)}";
    }

}
