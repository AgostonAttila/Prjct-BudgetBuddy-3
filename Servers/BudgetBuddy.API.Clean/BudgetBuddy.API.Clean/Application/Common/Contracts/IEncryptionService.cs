namespace BudgetBuddy.Application.Common.Contracts;

/// <summary>
/// Service for encrypting and decrypting sensitive data in the database
/// Uses ASP.NET Core Data Protection API for encryption at rest
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plaintext data
    /// </summary>
    /// <param name="plainText">Data to encrypt (can be null)</param>
    /// <param name="purpose">Purpose string for key isolation (e.g., "TransactionPayee", "TransactionNote")</param>
    /// <returns>Encrypted base64 string, or null if input is null</returns>
    string? Encrypt(string? plainText, string purpose);

    /// <summary>
    /// Decrypts encrypted data
    /// </summary>
    /// <param name="encryptedText">Encrypted base64 string (can be null)</param>
    /// <param name="purpose">Purpose string used during encryption</param>
    /// <returns>Decrypted plaintext, or null if input is null</returns>
    string? Decrypt(string? encryptedText, string purpose);
}
