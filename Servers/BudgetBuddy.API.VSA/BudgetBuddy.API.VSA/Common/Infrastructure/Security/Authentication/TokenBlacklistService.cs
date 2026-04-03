using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Security.Authentication;

/// <summary>
/// Token blacklist using raw token hash as cache key.
/// Works for any token type (JWT or opaque Identity bearer tokens) — no parsing required.
/// </summary>
public class TokenBlacklistService(
    IDistributedCache cache,
    ILogger<TokenBlacklistService> logger) : ITokenBlacklistService
{
    private const string BlacklistKeyPrefix = "blacklist:token:";
    private const string UserBlacklistKeyPrefix = "blacklist:user:";

    // Fallback TTL when the caller does not provide an explicit expiry
    private static readonly TimeSpan DefaultTokenTtl = TimeSpan.FromHours(24);

    public async Task RevokeTokenAsync(
        string token,
        DateTimeOffset? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ttl = expiresAt.HasValue
                ? expiresAt.Value - DateTimeOffset.UtcNow
                : DefaultTokenTtl;

            if (ttl <= TimeSpan.Zero)
            {
                logger.LogInformation("Token is already expired, no need to blacklist");
                return;
            }

            var cacheKey = $"{BlacklistKeyPrefix}{HashToken(token)}";
            await cache.SetStringAsync(cacheKey, "revoked",
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                cancellationToken);

            logger.LogInformation("Token revoked, TTL: {Ttl}", ttl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke token");
            throw;
        }
    }

    public async Task<bool> IsTokenRevokedAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{BlacklistKeyPrefix}{HashToken(token)}";
            var value = await cache.GetStringAsync(cacheKey, cancellationToken);
            return value is not null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check token blacklist");
            return true; // fail closed
        }
    }

    public async Task RevokeAllUserTokensAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{UserBlacklistKeyPrefix}{userId}";
            var timestamp = DateTimeOffset.UtcNow.ToString("o");
            await cache.SetStringAsync(cacheKey, timestamp,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) },
                cancellationToken);

            logger.LogWarning("All tokens revoked for user {UserId} at {Timestamp}", userId, timestamp);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke all user tokens for {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> AreAllUserTokensRevokedAsync(
        string userId,
        DateTimeOffset tokenIssuedAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{UserBlacklistKeyPrefix}{userId}";
            var revokedAtString = await cache.GetStringAsync(cacheKey, cancellationToken);

            if (string.IsNullOrEmpty(revokedAtString))
                return false;

            if (DateTimeOffset.TryParse(revokedAtString, out var revokedAt))
                return tokenIssuedAt < revokedAt;

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check user-level token revocation for {UserId}", userId);
            return true; // fail closed
        }
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
