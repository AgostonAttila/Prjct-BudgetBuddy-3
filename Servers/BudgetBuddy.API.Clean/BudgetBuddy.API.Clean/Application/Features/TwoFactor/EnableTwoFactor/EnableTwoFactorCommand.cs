using System.Security.Claims;

namespace BudgetBuddy.Application.Features.TwoFactor.EnableTwoFactor;

public record EnableTwoFactorCommand(
    ClaimsPrincipal User
) : IRequest<EnableTwoFactorResponse>;

public record EnableTwoFactorResponse(
    string SharedKey,
    string QrCodeDataUrl
);
