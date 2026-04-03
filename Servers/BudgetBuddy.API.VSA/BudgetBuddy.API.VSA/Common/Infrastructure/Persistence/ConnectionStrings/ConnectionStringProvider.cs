using BudgetBuddy.API.VSA.Common.Infrastructure.Security;
using BudgetBuddy.API.VSA.Common.Infrastructure.Security.Encryption;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.ConnectionStrings;

public class ConnectionStringProvider(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IDataProtectionService dataProtection) : IConnectionStringProvider
{
    private string? _protectedConnectionString;

    public string GetDbConnectionString()
    {
        if (_protectedConnectionString != null)
            return dataProtection.Unprotect(_protectedConnectionString);

        var connectionString = configuration["ConnectionStrings:DefaultConnection"]
                              ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");

        // Security validation: Ensure connection string is not hardcoded in appsettings.json files
        ValidateConnectionStringSource(connectionString);

        // Protect the connection string in memory using Data Protection API
        _protectedConnectionString = dataProtection.Protect(connectionString);

        return connectionString;
    }

    /// <summary>
    /// Validates that the connection string comes from a secure source
    /// Development: Environment variable
    /// Production: Azure Key Vault + SSL Mode required
    /// </summary>
    private void ValidateConnectionStringSource(string connectionString)
    {
        // In Development, connection string should come from environment variable
        // In Production, it should come from Azure Key Vault (validated by Key Vault URL presence)

        if (environment.IsProduction())
        {
            var keyVaultUrl = configuration["KeyVault:Url"];
            if (string.IsNullOrEmpty(keyVaultUrl))
            {
                throw new InvalidOperationException(
                    "SECURITY: Production environment requires Azure Key Vault configuration. " +
                    "Set KeyVault:Url in configuration.");
            }

            // Validate SSL Mode in production (must use Require, VerifyCA, or VerifyFull)
            if (!connectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase) &&
                !connectionString.Contains("SSL Mode=VerifyCA", StringComparison.OrdinalIgnoreCase) &&
                !connectionString.Contains("SSL Mode=VerifyFull", StringComparison.OrdinalIgnoreCase) &&
                !connectionString.Contains("SslMode=Require", StringComparison.OrdinalIgnoreCase) &&
                !connectionString.Contains("SslMode=VerifyCA", StringComparison.OrdinalIgnoreCase) &&
                !connectionString.Contains("SslMode=VerifyFull", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "SECURITY: Production connection string MUST use SSL Mode=Require or higher. " +
                    "Allowed modes: Require, VerifyCA, VerifyFull. " +
                    "Example: Host=...;SSL Mode=Require;Trust Server Certificate=false");
            }
        }

        // Warn if connection string appears to be hardcoded (contains actual values instead of placeholder)
        if (connectionString.Contains("your_password", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("placeholder", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "SECURITY: Connection string contains placeholder values. " +
                "Set ConnectionStrings__DefaultConnection environment variable (Development) or configure Azure Key Vault (Production).");
        }

        // Validate SSL Mode=Disable is never used (security risk)
        if (connectionString.Contains("SSL Mode=Disable", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("SslMode=Disable", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "SECURITY: SSL Mode=Disable is not allowed. Database connections must be encrypted!");
        }
    }
}