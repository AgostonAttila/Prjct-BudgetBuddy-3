using BudgetBuddy.Shared.Infrastructure.Security;
using BudgetBuddy.Shared.Infrastructure.Security.Encryption;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BudgetBuddy.Shared.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core Value Converter for transparent column encryption
/// Automatically encrypts data when saving to database and decrypts when reading
/// </summary>
public class EncryptedStringConverter(IEncryptionService encryptionService, string purpose)
    : ValueConverter<string?, string?>(plainText => encryptionService.Encrypt(plainText, purpose),
        encryptedText => encryptionService.Decrypt(encryptedText, purpose))
{
    // Encrypt when saving to database
    // Decrypt when reading from database
}
