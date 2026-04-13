namespace BudgetBuddy.Application.Common.Contracts;

/// <summary>
/// Service for managing revoked/blacklisted JWT tokens
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Adds a token to the blacklist (revokes it)
    /// </summary>
    /// <param name="token">The JWT token to revoke</param>
    /// <param name="expiresAt">When the token expires (optional, will be parsed from token if not provided)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeTokenAsync(string token, DateTimeOffset? expiresAt = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a token is blacklisted (revoked)
    /// </summary>
    /// <param name="token">The JWT token to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the token is blacklisted/revoked, false otherwise</returns>
    Task<bool> IsTokenRevokedAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeAllUserTokensAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all tokens for a user have been revoked
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="tokenIssuedAt">When the token was issued</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all user tokens issued before the revocation timestamp are revoked</returns>
    Task<bool> AreAllUserTokensRevokedAsync(string userId, DateTimeOffset tokenIssuedAt, CancellationToken cancellationToken = default);
}
