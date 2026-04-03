namespace BudgetBuddy.API.VSA.Features.Auth.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<TokenResponse>;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn);
