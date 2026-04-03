using Microsoft.AspNetCore.DataProtection;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Security.Encryption;

/// <summary>
/// Implementation of data protection using ASP.NET Core Data Protection API
/// Provides encryption/decryption for sensitive data like connection strings in memory
/// </summary>
public class DataProtectionService(IDataProtectionProvider dataProtectionProvider) : IDataProtectionService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("BudgetBuddy.ConnectionStrings.v1");

    // Create a protector with a specific purpose string
    // This ensures data encrypted for one purpose cannot be decrypted for another

    /// <summary>
    /// Encrypts a plain text string using Data Protection API
    /// </summary>
    public string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentNullException(nameof(plainText));

        return _protector.Protect(plainText);
    }

    /// <summary>
    /// Decrypts an encrypted string using Data Protection API
    /// </summary>
    public string Unprotect(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            throw new ArgumentNullException(nameof(encryptedText));

        return _protector.Unprotect(encryptedText);
    }
}
