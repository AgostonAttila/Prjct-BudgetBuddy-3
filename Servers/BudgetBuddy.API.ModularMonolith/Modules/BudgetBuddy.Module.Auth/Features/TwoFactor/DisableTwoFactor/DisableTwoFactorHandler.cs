using BudgetBuddy.Module.Auth.Services;
using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.Module.Auth.Features.TwoFactor.DisableTwoFactor;

public class DisableTwoFactorHandler(
    UserManager<User> userManager,
    IAuthenticationEmailService authEmailService,
    ILogger<DisableTwoFactorHandler> logger) : IRequestHandler<DisableTwoFactorCommand, DisableTwoFactorResponse>
{
    public async Task<DisableTwoFactorResponse> Handle(
        DisableTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(request.User);
        if (user == null)
        {
            logger.LogWarning("User not found when attempting to disable 2FA");
            throw new UnauthorizedAccessException("User not found");
        }

        logger.LogInformation("Disabling 2FA for user {UserId}", user.Id);

        // Verify password before disabling 2FA (security measure)
        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            logger.LogWarning("Invalid password provided when disabling 2FA for user {UserId}", user.Id);
            throw new ValidationException("Invalid password");
        }

        // Disable 2FA
        var result = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to disable 2FA for user {UserId}", user.Id);
            throw new DomainException("Failed to disable two-factor authentication");
        }

        // Reset authenticator key for security
        await userManager.ResetAuthenticatorKeyAsync(user);

        logger.LogInformation("2FA disabled successfully for user {UserId}", user.Id);

        // Send security alert email (fire-and-forget)
        var email = await userManager.GetEmailAsync(user);
        var userName = user.UserName ?? email ?? "User";
        _ = Task.Run(async () =>
        {
            await authEmailService.SendTwoFactorDisabledEmailAsync(email!, userName, cancellationToken);
        }, cancellationToken);

        return new DisableTwoFactorResponse(
            Success: true,
            Message: "Two-factor authentication has been disabled successfully"
        );
    }
}
