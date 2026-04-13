using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Features.TwoFactor.Services;

namespace BudgetBuddy.Application.Features.TwoFactor.DisableTwoFactor;

public class DisableTwoFactorHandler(
    ITwoFactorService twoFactorService,
    IAuthenticationEmailService authEmailService,
    ICurrentUserService currentUserService,
    ILogger<DisableTwoFactorHandler> logger)
    : UserAwareHandler<DisableTwoFactorCommand, DisableTwoFactorResponse>(currentUserService)
{
    public override async Task<DisableTwoFactorResponse> Handle(
        DisableTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Disabling 2FA for user {UserId}", UserId);

        var isPasswordValid = await twoFactorService.CheckPasswordAsync(UserId, request.Password, cancellationToken);
        if (!isPasswordValid)
        {
            logger.LogWarning("Invalid password provided when disabling 2FA for user {UserId}", UserId);
            throw new DomainValidationException("Invalid password");
        }

        await twoFactorService.DisableAsync(UserId, cancellationToken);

        logger.LogInformation("2FA disabled successfully for user {UserId}", UserId);

        var contact = await twoFactorService.GetContactInfoAsync(UserId, cancellationToken);
        _ = Task.Run(async () =>
        {
            await authEmailService.SendTwoFactorDisabledEmailAsync(contact.Email, contact.DisplayName, cancellationToken);
        }, cancellationToken);

        return new DisableTwoFactorResponse(
            Success: true,
            Message: "Two-factor authentication has been disabled successfully");
    }
}
