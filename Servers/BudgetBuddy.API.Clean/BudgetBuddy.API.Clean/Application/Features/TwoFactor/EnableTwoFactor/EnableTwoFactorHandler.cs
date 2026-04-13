using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Features.TwoFactor.Services;

namespace BudgetBuddy.Application.Features.TwoFactor.EnableTwoFactor;

public class EnableTwoFactorHandler(
    ITwoFactorService twoFactorService,
    ICurrentUserService currentUserService,
    ILogger<EnableTwoFactorHandler> logger)
    : UserAwareHandler<EnableTwoFactorCommand, EnableTwoFactorResponse>(currentUserService)
{
    public override async Task<EnableTwoFactorResponse> Handle(
        EnableTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Generating 2FA setup for user {UserId}", UserId);

        var setupData = await twoFactorService.GetSetupDataAsync(UserId, cancellationToken);

        logger.LogInformation("2FA setup generated successfully for user {UserId}", UserId);

        return new EnableTwoFactorResponse(
            SharedKey: setupData.SharedKey,
            QrCodeDataUrl: setupData.QrCodeDataUrl);
    }
}
