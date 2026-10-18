using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ShareSmallBiz.Core.Auth;
using ShareSmallBiz.Core.Users;
using ShareSmallBiz.Infrastructure.Data;

namespace ShareSmallBiz.Api.Auth;

/// <summary>Issues access tokens and rotating refresh tokens, and revokes refresh-token families.</summary>
public sealed class TokenService(
    AppDbContext db,
    UserManager<AppUser> users,
    IOptions<JwtOptions> options,
    TimeProvider time)
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    /// <summary>Issues a new token pair. Pass <paramref name="familyId"/> to continue an existing session.</summary>
    public async Task<AuthTokensResponse> IssueAsync(AppUser user, Guid? familyId = null, CancellationToken cancellationToken = default)
    {
        var jwt = options.Value;
        var now = time.GetUtcNow();
        var accessExpires = now.AddMinutes(jwt.AccessTokenMinutes);
        var roles = await users.GetRolesAsync(user);

        var accessToken = TokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = accessExpires.UtcDateTime,
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id,
                [JwtRegisteredClaimNames.Name] = user.UserName ?? string.Empty,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
                [AuthConstants.RoleClaim] = roles.ToArray(),
            },
        });

        var refreshToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var refreshExpires = now.AddDays(jwt.RefreshTokenDays);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(refreshToken),
            FamilyId = familyId ?? Guid.NewGuid(),
            CreatedAt = now,
            ExpiresAt = refreshExpires,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new AuthTokensResponse("Bearer", accessToken, accessExpires, refreshToken, refreshExpires);
    }

    /// <summary>
    /// Exchanges a refresh token for a new pair, or returns null. Presenting an already-used token
    /// is treated as theft and revokes every token in its family.
    /// </summary>
    public async Task<AuthTokensResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await FindAsync(refreshToken, cancellationToken);
        if (stored is null)
            return null;

        var now = time.GetUtcNow();
        if (stored.RevokedAt is not null)
        {
            await RevokeFamilyAsync(stored.FamilyId, cancellationToken);
            return null;
        }
        if (stored.ExpiresAt <= now)
            return null;

        // Atomic claim of the token: a concurrent request presenting the same token loses and is treated as reuse
        var claimed = await db.RefreshTokens
            .Where(t => t.Id == stored.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
        if (claimed == 0)
        {
            await RevokeFamilyAsync(stored.FamilyId, cancellationToken);
            return null;
        }

        var user = await users.FindByIdAsync(stored.UserId);
        if (user is null || await users.IsLockedOutAsync(user))
        {
            await RevokeFamilyAsync(stored.FamilyId, cancellationToken);
            return null;
        }

        return await IssueAsync(user, stored.FamilyId, cancellationToken);
    }

    /// <summary>Ends the session the refresh token belongs to. Unknown tokens are ignored.</summary>
    public async Task RevokeSessionAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await FindAsync(refreshToken, cancellationToken);
        if (stored is not null)
            await RevokeFamilyAsync(stored.FamilyId, cancellationToken);
    }

    /// <summary>Ends every session for the user (e.g. after a password reset).</summary>
    public Task RevokeAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        return db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
    }

    private async Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
    }

    private Task<RefreshToken?> FindAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = Hash(refreshToken);
        return db.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
