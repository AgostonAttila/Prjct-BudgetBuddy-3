using BudgetBuddy.Infrastructure.Security;
using BudgetBuddy.Infrastructure.Security.Encryption;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BudgetBuddy.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core Value Converter for transparent column encryption
/// Automatically encrypts data when saving to database and decrypts when reading
/// </summary>
public class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(IEncryptionService encryptionService, string purpose)
        : base(
            // Encrypt when saving to database
            plainText => encryptionService.Encrypt(plainText, purpose),
            // Decrypt when reading from database
            encryptedText => encryptionService.Decrypt(encryptedText, purpose))
    {
    }
}
