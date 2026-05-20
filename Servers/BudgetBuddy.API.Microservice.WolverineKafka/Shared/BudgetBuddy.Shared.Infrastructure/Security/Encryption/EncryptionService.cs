using Microsoft.AspNetCore.DataProtection;

namespace BudgetBuddy.Shared.Infrastructure.Security.Encryption;

/// <summary>
/// Encryption service using ASP.NET Core Data Protection API
/// Provides transparent encryption/decryption for sensitive database columns
/// </summary>
public class EncryptionService(IDataProtectionProvider dataProtectionProvider,ILogger<EncryptionService> logger) : IEncryptionService
{
   

    public string? Encrypt(string? plainText, string purpose)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        try
        {
            var protector = dataProtectionProvider.CreateProtector($"BudgetBuddy.ColumnEncryption.{purpose}");
            var encrypted = protector.Protect(plainText);

            logger.LogTrace("Encrypted data for purpose: {Purpose}", purpose);
            return encrypted;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to encrypt data for purpose '{purpose}'.", ex);
        }
    }

    public string? Decrypt(string? encryptedText, string purpose)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return encryptedText;
        }

        try
        {
            var protector = dataProtectionProvider.CreateProtector($"BudgetBuddy.ColumnEncryption.{purpose}");
            var decrypted = protector.Unprotect(encryptedText);

            logger.LogTrace("Decrypted data for purpose: {Purpose}", purpose);
            return decrypted;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to decrypt data for purpose '{purpose}'. Data may be corrupted or encryption keys were rotated without migration.", ex);
        }
    }
}
