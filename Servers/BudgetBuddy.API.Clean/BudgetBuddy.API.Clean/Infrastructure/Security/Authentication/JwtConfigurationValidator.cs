using System.Text.RegularExpressions;

namespace BudgetBuddy.Infrastructure.Security.Authentication;

/// <summary>
/// Validates JWT configuration at startup to prevent common security misconfigurations
/// </summary>
public static class JwtConfigurationValidator
{
    private static readonly string[] InsecureSecretKeys = new[]
    {
        "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
        "your_secret_key",
        "secret",
        "mysecret",
        "test",
        "password",
        "12345",
        "admin"
    };

    /// <summary>
    /// Validates JWT secret key meets security requirements
    /// </summary>
    /// <param name="secretKey">The JWT secret key</param>
    /// <param name="environment">Current environment name</param>
    /// <exception cref="InvalidOperationException">Thrown if validation fails</exception>
    public static void ValidateSecretKey(string? secretKey, string environment)
    {
        var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
        var isProduction = environment.Equals("Production", StringComparison.OrdinalIgnoreCase);

        // 1. Check if secret key exists
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "❌ JWT Secret Key is not configured! " +
                "Set 'Jwt:SecretKey' in appsettings.json or environment variable 'Jwt__SecretKey'.");
        }

        // 2. Check minimum length (256 bits = 32 bytes)
        if (secretKey.Length < 32)
        {
            throw new InvalidOperationException(
                $"❌ JWT Secret Key is too short ({secretKey.Length} characters). " +
                "Minimum required: 32 characters (256 bits). " +
                "Recommended: 64 characters (512 bits) for HS512.");
        }

        // 3. Check for common insecure keys
        foreach (var insecureKey in InsecureSecretKeys)
        {
            if (secretKey.Contains(insecureKey, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"❌ JWT Secret Key contains insecure pattern: '{insecureKey}'. " +
                    "Generate a cryptographically secure random key using: " +
                    "dotnet user-secrets set 'Jwt:SecretKey' \"$(openssl rand -base64 64)\"");
            }
        }

        // 4. Check entropy (not too simple)
        if (IsLowEntropy(secretKey))
        {
            var message = isDevelopment
                ? "⚠️ WARNING: JWT Secret Key has low entropy (too simple/repetitive). " +
                  "Development mode: Continuing, but generate a secure key for production!"
                : "❌ JWT Secret Key has low entropy (too simple/repetitive). " +
                  "Generate a cryptographically secure random key.";

            if (!isDevelopment)
            {
                throw new InvalidOperationException(message);
            }

            // Log warning in development
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        // 5. Production-specific checks
        if (isProduction)
        {
            // Check if key looks like it came from Azure Key Vault or environment variable
            // (not from appsettings.json)
            if (secretKey.Length < 64)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    "⚠️ WARNING: Production JWT Secret Key is shorter than recommended (64+ characters). " +
                    "Consider using a 512-bit key.");
                Console.ResetColor();
            }

            // Recommend using Azure Key Vault
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(
                "💡 RECOMMENDATION: Store JWT Secret Key in Azure Key Vault for production. " +
                "See: https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Checks if a string has low entropy (too simple or repetitive)
    /// </summary>
    private static bool IsLowEntropy(string value)
    {
        // Check for repeated characters (e.g., "aaaaaaa...")
        if (Regex.IsMatch(value, @"(.)\1{5,}"))
            return true;

        // Check for sequential characters (e.g., "abcdef", "123456")
        if (HasSequentialCharacters(value, 6))
            return true;

        // Check for common patterns
        if (Regex.IsMatch(value, @"(password|secret|admin|test)", RegexOptions.IgnoreCase))
            return true;

        // Check for too few unique characters (should have variety)
        var uniqueChars = value.Distinct().Count();
        var uniqueRatio = (double)uniqueChars / value.Length;
        if (uniqueRatio < 0.3) // Less than 30% unique characters
            return true;

        return false;
    }

    /// <summary>
    /// Checks if string contains sequential characters
    /// </summary>
    private static bool HasSequentialCharacters(string value, int sequenceLength)
    {
        for (int i = 0; i < value.Length - sequenceLength + 1; i++)
        {
            bool isSequential = true;
            for (int j = 0; j < sequenceLength - 1; j++)
            {
                if (value[i + j + 1] != value[i + j] + 1)
                {
                    isSequential = false;
                    break;
                }
            }
            if (isSequential)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Generates a cryptographically secure random secret key
    /// </summary>
    /// <param name="lengthInBytes">Length in bytes (default: 64 = 512 bits)</param>
    /// <returns>Base64-encoded random key</returns>
    public static string GenerateSecureKey(int lengthInBytes = 64)
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[lengthInBytes];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
