using System.Security.Cryptography;
using System.Text;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Auth.RefreshToken;

public class RefreshTokenHandler(
    IRefreshTokenRepository refreshTokenRepo,
    IUnitOfWork uow,
    ITokenService tokenService,
    ILogger<RefreshTokenHandler> logger)
    : IRequestHandler<RefreshTokenCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var ipAddress = request.IpAddress;

        logger.LogInformation("Refresh token request from IP: {IpAddress}", ipAddress);

        // 1. Hash the incoming token (database stores hashed tokens)
        var hashedToken = HashToken(request.RefreshToken);

        // 2. Find the refresh token in database
        var oldToken = await refreshTokenRepo.GetByTokenHashAsync(hashedToken, cancellationToken);

        if (oldToken == null)
        {
            logger.LogWarning("Refresh token not found: {Token}", request.RefreshToken);
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // 3. Check if token is active (not expired and not revoked)
        if (!oldToken.IsActive)
        {
            logger.LogWarning(
                "Refresh token is inactive. Expired: {IsExpired}, Revoked: {IsRevoked}, UserId: {UserId}",
                oldToken.IsExpired,
                oldToken.IsRevoked,
                oldToken.UserId);

            throw new UnauthorizedAccessException("Refresh token is expired or revoked");
        }

        // 4. Check for token reuse (SECURITY BREACH DETECTION!)
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

            await RevokeTokenFamilyAsync(
                oldToken.UserId,
                ipAddress,
                "Token reuse detected - security breach",
                cancellationToken);

            throw new UnauthorizedAccessException(
                "Token reuse detected. All refresh tokens have been revoked for security. Please login again.");
        }

        // 5. Generate new token pair
        var newAccessToken = tokenService.GenerateAccessToken(oldToken.User);
        var newRefreshToken = tokenService.CreateRefreshToken(oldToken.User, ipAddress);

        // 6. Token rotation: Mark old token as replaced
        oldToken.ReplacedByToken = newRefreshToken.Token;
        oldToken.RevokedAt = DateTime.UtcNow;
        oldToken.RevokedReason = "Replaced by new token";
        oldToken.RevokedByIp = ipAddress;

        // 7. Save new refresh token to database
        await refreshTokenRepo.AddAsync(newRefreshToken, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Refresh token rotated successfully for user {UserId}. Old token ID: {OldTokenId}, New token ID: {NewTokenId}",
            oldToken.UserId,
            oldToken.Id,
            newRefreshToken.Id);

        // Note: Return PLAIN token to client (not hashed)
        return new TokenResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken.PlainToken ?? newRefreshToken.Token,
            TokenType: "Bearer",
            ExpiresIn: 900 // 15 minutes in seconds
        );
    }

    private static string HashToken(string plainToken)
    {
        using var sha256 = SHA256.Create();
        var tokenBytes = Encoding.UTF8.GetBytes(plainToken);
        var hashBytes = sha256.ComputeHash(tokenBytes);
        return Convert.ToBase64String(hashBytes);
    }

    private async Task RevokeTokenFamilyAsync(
        string userId,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken)
    {
        var activeTokens = await refreshTokenRepo.GetActiveByUserIdAsync(userId, cancellationToken);

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

        await uow.SaveChangesAsync(cancellationToken);
    }
}
