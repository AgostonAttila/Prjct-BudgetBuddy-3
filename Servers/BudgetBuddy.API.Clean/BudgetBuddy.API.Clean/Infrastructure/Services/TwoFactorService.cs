using BudgetBuddy.Application.Features.TwoFactor.Services;
using Microsoft.AspNetCore.Identity;
using QRCoder;

namespace BudgetBuddy.Infrastructure.Services;

public class TwoFactorService(
    UserManager<User> userManager,
    ILogger<TwoFactorService> logger) : ITwoFactorService
{
    public async Task<TwoFactorSetupData> GetSetupDataAsync(string userId, CancellationToken ct = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = await userManager.GetEmailAsync(user);
        var authenticatorUri = BuildAuthenticatorUri(email!, key!);
        var qrCodeDataUrl = GenerateQrCodeImage(authenticatorUri);

        logger.LogInformation("2FA setup data generated for user {UserId}", userId);

        return new TwoFactorSetupData(
            SharedKey: FormatKey(key!),
            QrCodeDataUrl: qrCodeDataUrl);
    }

    public async Task<TwoFactorStatus> GetStatusAsync(string userId, CancellationToken ct = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        var isEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        var authenticatorKey = await userManager.GetAuthenticatorKeyAsync(user);

        return new TwoFactorStatus(
            IsEnabled: isEnabled,
            HasAuthenticator: !string.IsNullOrEmpty(authenticatorKey));
    }

    public async Task EnableAsync(string userId, string verificationCode, CancellationToken ct = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            verificationCode);

        if (!isValid)
        {
            logger.LogWarning("Invalid 2FA verification code for user {UserId}", userId);
            throw new DomainValidationException("Invalid verification code. Please try again.");
        }

        var result = await userManager.SetTwoFactorEnabledAsync(user, true);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to enable 2FA for user {UserId}", userId);
            throw new DomainException("Failed to enable two-factor authentication");
        }

        logger.LogInformation("2FA enabled for user {UserId}", userId);
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password, CancellationToken ct = default)
    {
        var user = await FindUserOrThrowAsync(userId);
        return await userManager.CheckPasswordAsync(user, password);
    }

    public async Task DisableAsync(string userId, CancellationToken ct = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        var result = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to disable 2FA for user {UserId}", userId);
            throw new DomainException("Failed to disable two-factor authentication");
        }

        await userManager.ResetAuthenticatorKeyAsync(user);

        logger.LogInformation("2FA disabled for user {UserId}", userId);
    }

    public async Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(
        string userId, int count, CancellationToken ct = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, count);

        logger.LogWarning("New recovery codes generated for user {UserId} — old codes invalidated", userId);

        return codes!.ToList().AsReadOnly();
    }

    public async Task<TwoFactorContactInfo> GetContactInfoAsync(string userId, CancellationToken ct = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        var email = await userManager.GetEmailAsync(user);
        var displayName = user.UserName ?? email ?? "User";

        return new TwoFactorContactInfo(Email: email!, DisplayName: displayName);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers

    private async Task<User> FindUserOrThrowAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            logger.LogWarning("User {UserId} not found in TwoFactorService", userId);
            throw new UnauthorizedAccessException("User not found");
        }

        return user;
    }

    private static string BuildAuthenticatorUri(string email, string key)
    {
        const string issuer = "BudgetBuddy";
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
               $"?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
    }

    private static string GenerateQrCodeImage(string authenticatorUri)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
    }

    private static string FormatKey(string key)
    {
        const int chunkSize = 4;
        var chunks = new List<string>();
        for (int i = 0; i < key.Length; i += chunkSize)
            chunks.Add(key.Substring(i, Math.Min(chunkSize, key.Length - i)));
        return string.Join(" ", chunks);
    }
}
