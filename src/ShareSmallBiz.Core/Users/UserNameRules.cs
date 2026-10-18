using System.Text.RegularExpressions;

namespace ShareSmallBiz.Core.Users;

/// <summary>Rules for public profile slugs (used as <c>UserName</c> and in profile URLs).</summary>
public static partial class UserNameRules
{
    public const int MinLength = 3;
    public const int MaxLength = 30;

    /// <summary>Lowercase letters, digits and single hyphens; must start and end with a letter or digit.</summary>
    public const string Pattern = "^[a-z0-9](?:[a-z0-9]|-(?=[a-z0-9]))*$";

    /// <summary>Characters ASP.NET Identity accepts in a user name; the pattern above is stricter.</summary>
    public const string AllowedCharacters = "abcdefghijklmnopqrstuvwxyz0123456789-";

    public static bool IsValid(string? userName) =>
        userName is { Length: >= MinLength and <= MaxLength } && SlugRegex().IsMatch(userName);

    [GeneratedRegex(Pattern, RegexOptions.CultureInvariant)]
    private static partial Regex SlugRegex();
}
