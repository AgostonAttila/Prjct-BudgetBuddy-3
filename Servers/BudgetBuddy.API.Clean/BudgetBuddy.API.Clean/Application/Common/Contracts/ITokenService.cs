
namespace BudgetBuddy.Application.Common.Contracts;

public interface ITokenService
{
    /// <summary>
    /// Generates a new access token (JWT) for the user
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically secure refresh token
    /// </summary>
    string GenerateRefreshTokenString();

    /// <summary>
    /// Creates a new RefreshToken entity
    /// </summary>
    RefreshToken CreateRefreshToken(User user, string? ipAddress);
}
