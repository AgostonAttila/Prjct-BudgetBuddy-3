namespace BudgetBuddy.Application.Features.Auth.RefreshToken;

public record RefreshTokenCommand(string RefreshToken, string? IpAddress) : IRequest<TokenResponse>;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn);
