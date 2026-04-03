using System.Security.Claims;

namespace BudgetBuddy.API.VSA.Features.TwoFactor.GetTwoFactorStatus;

public record GetTwoFactorStatusQuery(
    ClaimsPrincipal User
) : IRequest<TwoFactorStatusResponse>;

public record TwoFactorStatusResponse(
    bool IsEnabled,
    bool HasAuthenticator
);
