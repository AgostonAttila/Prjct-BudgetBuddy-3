namespace BudgetBuddy.Application.Common.Contracts;

/// <summary>
/// Service for encrypting and decrypting sensitive data using ASP.NET Core Data Protection API
/// </summary>
public interface IDataProtectionService
{
    /// <summary>
    /// Encrypts a plain text string
    /// </summary>
    string Protect(string plainText);

    /// <summary>
    /// Decrypts an encrypted string
    /// </summary>
    string Unprotect(string encryptedText);
}
