namespace BudgetBuddy.Module.Auth.Features.Authentication.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<TokenResponse>;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn);
