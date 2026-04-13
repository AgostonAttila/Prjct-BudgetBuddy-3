using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Features.TwoFactor.Services;

namespace BudgetBuddy.Application.Features.TwoFactor.VerifyTwoFactorSetup;

public class VerifyTwoFactorSetupHandler(
    ITwoFactorService twoFactorService,
    IAuthenticationEmailService authEmailService,
    ICurrentUserService currentUserService,
    ILogger<VerifyTwoFactorSetupHandler> logger)
    : UserAwareHandler<VerifyTwoFactorSetupCommand, VerifyTwoFactorSetupResponse>(currentUserService)
{
    public override async Task<VerifyTwoFactorSetupResponse> Handle(
        VerifyTwoFactorSetupCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Verifying 2FA setup for user {UserId}", UserId);

        // EnableAsync verifies the code and enables 2FA atomically; throws DomainValidationException if code is invalid
        await twoFactorService.EnableAsync(UserId, request.Code, cancellationToken);

        logger.LogInformation("2FA enabled successfully for user {UserId}", UserId);

        var contact = await twoFactorService.GetContactInfoAsync(UserId, cancellationToken);
        _ = Task.Run(async () =>
        {
            await authEmailService.SendTwoFactorEnabledEmailAsync(contact.Email, contact.DisplayName, cancellationToken);
        }, cancellationToken);

        return new VerifyTwoFactorSetupResponse(
            Success: true,
            Message: "Two-factor authentication has been enabled successfully");
    }
}
