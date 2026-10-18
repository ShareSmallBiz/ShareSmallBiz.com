namespace ShareSmallBiz.Core.Auth;

/// <summary>
/// A refresh token issued to one client session. Only a SHA-256 hash of the token is stored.
/// Tokens rotate on every use; all tokens from one login share a <see cref="FamilyId"/> so that
/// replaying an already-used token revokes the whole session.
/// </summary>
public class RefreshToken
{
    public long Id { get; set; }

    public required string UserId { get; set; }

    public required string TokenHash { get; set; }

    public Guid FamilyId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
