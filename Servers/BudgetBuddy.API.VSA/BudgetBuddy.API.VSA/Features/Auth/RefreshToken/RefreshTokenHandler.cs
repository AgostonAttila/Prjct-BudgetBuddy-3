using System.Security.Cryptography;
using System.Text;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Infrastructure.Security.Authentication;
using BudgetBuddy.API.VSA.Common.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.API.VSA.Features.Auth.RefreshToken;

public class RefreshTokenHandler(
    AppDbContext dbContext,
    ITokenService tokenService,
    ILogger<RefreshTokenHandler> logger,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<RefreshTokenCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        logger.LogInformation("Refresh token request from IP: {IpAddress}", ipAddress);

        // 1. Hash the incoming token (database stores hashed tokens)
        var hashedToken = HashToken(request.RefreshToken);

        // 2. Find the refresh token in database
        var oldToken = await dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == hashedToken, cancellationToken);

        if (oldToken == null)
        {
            logger.LogWarning("Refresh token not found: {Token}", request.RefreshToken);
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // 2. Check if token is active (not expired and not revoked)
        if (!oldToken.IsActive)
        {
            logger.LogWarning(
                "Refresh token is inactive. Expired: {IsExpired}, Revoked: {IsRevoked}, UserId: {UserId}",
                oldToken.IsExpired,
                oldToken.IsRevoked,
                oldToken.UserId);

            throw new UnauthorizedAccessException("Refresh token is expired or revoked");
        }

        // 3.  Check for token reuse (SECURITY BREACH DETECTION!)
        if (oldToken.ReplacedByToken != null)
        {
            logger.LogCritical(
                "🚨 SECURITY ALERT: Token reuse detected! " +
                "Token {Token} was already replaced by {ReplacedBy}. " +
                "User: {UserId}, IP: {IpAddress}. " +
                "REVOKING ALL TOKENS FOR THIS USER!",
                request.RefreshToken,
                oldToken.ReplacedByToken,
                oldToken.UserId,
                ipAddress);

            // Revoke all active tokens for this user (token family)
            await RevokeTokenFamilyAsync(
                oldToken.UserId,
                ipAddress,
                "Token reuse detected - security breach",
                cancellationToken);

            throw new UnauthorizedAccessException(
                "Token reuse detected. All refresh tokens have been revoked for security. Please login again.");
        }

        // 4. Generate new token pair
        var newAccessToken = tokenService.GenerateAccessToken(oldToken.User);
        var newRefreshToken = tokenService.CreateRefreshToken(oldToken.User, ipAddress);

        // 5. Token rotation: Mark old token as replaced
        // Note: newRefreshToken.Token is already hashed by EF converter
        oldToken.ReplacedByToken = newRefreshToken.Token;
        oldToken.RevokedAt = DateTime.UtcNow;
        oldToken.RevokedReason = "Replaced by new token";
        oldToken.RevokedByIp = ipAddress;

        // 6. Save new refresh token to database
        await dbContext.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Refresh token rotated successfully for user {UserId}. Old token ID: {OldTokenId}, New token ID: {NewTokenId}",
            oldToken.UserId,
            oldToken.Id,
            newRefreshToken.Id);

        // Note: Return PLAIN token to client (not hashed)
        // newRefreshToken.Token is hashed in DB, but PlainToken has the original
        return new TokenResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken.PlainToken ?? newRefreshToken.Token,  // Plain token for client
            TokenType: "Bearer",
            ExpiresIn: 900 // 15 minutes in seconds
        );
    }

    /// <summary>
    /// Hashes a token using SHA256 (same algorithm as HashedStringConverter)
    /// </summary>
    private static string HashToken(string plainToken)
    {
        using var sha256 = SHA256.Create();
        var tokenBytes = Encoding.UTF8.GetBytes(plainToken);
        var hashBytes = sha256.ComputeHash(tokenBytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Revokes all active refresh tokens for a user (token family revocation)
    /// Called when token reuse is detected (security breach)
    /// </summary>
    private async Task RevokeTokenFamilyAsync(
        string userId,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        logger.LogWarning(
            "Revoking {Count} active tokens for user {UserId} due to: {Reason}",
            activeTokens.Count,
            userId,
            reason);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = reason;
            token.RevokedByIp = ipAddress;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
