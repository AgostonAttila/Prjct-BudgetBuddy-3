namespace BudgetBuddy.Application.Features.TwoFactor.Services;

public record TwoFactorSetupData(string SharedKey, string QrCodeDataUrl);
public record TwoFactorStatus(bool IsEnabled, bool HasAuthenticator);
public record TwoFactorContactInfo(string Email, string DisplayName);

public interface ITwoFactorService
{
    /// <summary>Gets authenticator key and QR code for 2FA setup.</summary>
    Task<TwoFactorSetupData> GetSetupDataAsync(string userId, CancellationToken ct = default);

    /// <summary>Returns whether 2FA is enabled and whether an authenticator key exists.</summary>
    Task<TwoFactorStatus> GetStatusAsync(string userId, CancellationToken ct = default);

    /// <summary>Verifies the TOTP code and enables 2FA. Throws DomainValidationException if code is invalid.</summary>
    Task EnableAsync(string userId, string verificationCode, CancellationToken ct = default);

    /// <summary>Checks whether the given password is correct for the user.</summary>
    Task<bool> CheckPasswordAsync(string userId, string password, CancellationToken ct = default);

    /// <summary>Disables 2FA and resets the authenticator key.</summary>
    Task DisableAsync(string userId, CancellationToken ct = default);

    /// <summary>Generates new recovery codes (invalidates old ones).</summary>
    Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(string userId, int count, CancellationToken ct = default);

    /// <summary>Returns e-mail address and display name for security notification emails.</summary>
    Task<TwoFactorContactInfo> GetContactInfoAsync(string userId, CancellationToken ct = default);
}
