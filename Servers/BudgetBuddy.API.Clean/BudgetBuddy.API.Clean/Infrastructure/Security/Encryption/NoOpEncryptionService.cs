namespace BudgetBuddy.Infrastructure.Security.Encryption;

/// <summary>
/// Pass-through implementation of IEncryptionService used only at EF Core design-time
/// (migrations). Converters affect data serialization, not schema, so migrations work
/// correctly without real encryption.
/// </summary>
internal sealed class NoOpEncryptionService : IEncryptionService
{
    public string? Encrypt(string? plainText, string purpose) => plainText;
    public string? Decrypt(string? encryptedText, string purpose) => encryptedText;
}
