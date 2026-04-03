using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core Value Converter for one-way hashing of tokens
/// Automatically hashes data when saving to database
///
/// DIFFERENCE from EncryptedStringConverter:
/// - Encryption: Two-way (encrypt + decrypt), non-deterministic
/// - Hashing: One-way (hash only), deterministic
///
/// USE CASES:
/// - EncryptedStringConverter: Sensitive data that needs to be retrieved (Payee, Notes)
/// - HashedStringConverter: Tokens that only need comparison (RefreshTokens, API Keys)
///
/// WHY DETERMINISTIC MATTERS:
/// - Encryption with random IV: Same input → Different output (can't search in DB)
/// - Hashing: Same input → Same output (can search with WHERE clause)
/// </summary>
public class HashedStringConverter : ValueConverter<string?, string?>
{
    public HashedStringConverter()
        : base(
            // Hash when saving to database
            plainText => HashToken(plainText),
            // Cannot unhash - return hash as-is when reading
            // NOTE: You should never need to read the hash directly
            hashedText => hashedText)
    {
    }

    private static string? HashToken(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var sha256 = SHA256.Create();
        var tokenBytes = Encoding.UTF8.GetBytes(plainText);
        var hashBytes = sha256.ComputeHash(tokenBytes);

        return Convert.ToBase64String(hashBytes);
    }
}
