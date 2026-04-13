using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Features.TwoFactor.Services;

namespace BudgetBuddy.Application.Features.TwoFactor.GetTwoFactorStatus;

public class GetTwoFactorStatusHandler(
    ITwoFactorService twoFactorService,
    ICurrentUserService currentUserService,
    ILogger<GetTwoFactorStatusHandler> logger)
    : UserAwareHandler<GetTwoFactorStatusQuery, TwoFactorStatusResponse>(currentUserService)
{
    public override async Task<TwoFactorStatusResponse> Handle(
        GetTwoFactorStatusQuery request,
        CancellationToken cancellationToken)
    {
        var status = await twoFactorService.GetStatusAsync(UserId, cancellationToken);

        return new TwoFactorStatusResponse(
            IsEnabled: status.IsEnabled,
            HasAuthenticator: status.HasAuthenticator);
    }
}
