using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.API.VSA.Features.TwoFactor.GetTwoFactorStatus;

public class GetTwoFactorStatusHandler(
    UserManager<User> userManager,
    ILogger<GetTwoFactorStatusHandler> logger) : IRequestHandler<GetTwoFactorStatusQuery, TwoFactorStatusResponse>
{
    public async Task<TwoFactorStatusResponse> Handle(
        GetTwoFactorStatusQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(request.User);
        if (user == null)
        {
            logger.LogWarning("User not found when checking 2FA status");
            throw new UnauthorizedAccessException("User not found");
        }

        var isEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        var authenticatorKey = await userManager.GetAuthenticatorKeyAsync(user);
        var hasAuthenticator = !string.IsNullOrEmpty(authenticatorKey);

        return new TwoFactorStatusResponse(
            IsEnabled: isEnabled,
            HasAuthenticator: hasAuthenticator
        );
    }
}
