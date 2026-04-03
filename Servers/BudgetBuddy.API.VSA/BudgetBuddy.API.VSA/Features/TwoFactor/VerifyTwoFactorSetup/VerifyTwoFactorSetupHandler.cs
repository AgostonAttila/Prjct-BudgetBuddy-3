using BudgetBuddy.API.VSA.Common.Infrastructure.Notification.Email;
using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.API.VSA.Features.TwoFactor.VerifyTwoFactorSetup;

public class VerifyTwoFactorSetupHandler(
    UserManager<User> userManager,
    IAuthenticationEmailService authEmailService,
    ILogger<VerifyTwoFactorSetupHandler> logger) : IRequestHandler<VerifyTwoFactorSetupCommand, VerifyTwoFactorSetupResponse>
{
    public async Task<VerifyTwoFactorSetupResponse> Handle(
        VerifyTwoFactorSetupCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(request.User);
        if (user == null)
        {
            logger.LogWarning("User not found when attempting to verify 2FA setup");
            throw new UnauthorizedAccessException("User not found");
        }

        logger.LogInformation("Verifying 2FA setup for user {UserId}", user.Id);

        // Verify the TOTP code
        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            request.Code);

        if (!isValid)
        {
            logger.LogWarning("Invalid 2FA verification code for user {UserId}", user.Id);
            throw new ValidationException("Invalid verification code. Please try again.");
        }

        // Enable 2FA for the user
        var result = await userManager.SetTwoFactorEnabledAsync(user, true);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to enable 2FA for user {UserId}", user.Id);
            throw new DomainException("Failed to enable two-factor authentication");
        }

        logger.LogInformation("2FA enabled successfully for user {UserId}", user.Id);

        // Send confirmation email (fire-and-forget, don't block the response)
        var email = await userManager.GetEmailAsync(user);
        var userName = user.UserName ?? email ?? "User";
        _ = Task.Run(async () =>
        {
            await authEmailService.SendTwoFactorEnabledEmailAsync(email!, userName, cancellationToken);
        }, cancellationToken);

        return new VerifyTwoFactorSetupResponse(
            Success: true,
            Message: "Two-factor authentication has been enabled successfully"
        );
    }
}
