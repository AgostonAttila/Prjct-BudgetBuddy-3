using System.Security.Claims;

namespace BudgetBuddy.Module.Auth.Features.TwoFactor.GetTwoFactorStatus;

public record GetTwoFactorStatusQuery(
    ClaimsPrincipal User
) : IRequest<TwoFactorStatusResponse>;

public record TwoFactorStatusResponse(
    bool IsEnabled,
    bool HasAuthenticator
);
