using System.Security.Claims;

namespace BudgetBuddy.Module.Auth.Features.TwoFactor.EnableTwoFactor;

public record EnableTwoFactorCommand(
    ClaimsPrincipal User
) : IRequest<EnableTwoFactorResponse>;

public record EnableTwoFactorResponse(
    string SharedKey,
    string QrCodeDataUrl
);
