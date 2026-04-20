namespace BudgetBuddy.Module.Auth.Infrastructure.Authentication;

/// <summary>
/// Service for managing revoked/blacklisted JWT tokens
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Adds a token to the blacklist (revokes it)
    /// </summary>
    Task RevokeTokenAsync(string token, DateTimeOffset? expiresAt = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a token is blacklisted (revoked)
    /// </summary>
    Task<bool> IsTokenRevokedAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens for a specific user
    /// </summary>
    Task RevokeAllUserTokensAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all tokens for a user have been revoked
    /// </summary>
    Task<bool> AreAllUserTokensRevokedAsync(string userId, DateTimeOffset tokenIssuedAt, CancellationToken cancellationToken = default);
}
