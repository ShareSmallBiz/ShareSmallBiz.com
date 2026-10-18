namespace ShareSmallBiz.Api.Auth;

internal static class AuthConstants
{
    /// <summary>Short JWT claim name for roles; inbound claim mapping is disabled so it stays as-is.</summary>
    public const string RoleClaim = "role";

    public const string BearerSchemeName = "Bearer";
}
