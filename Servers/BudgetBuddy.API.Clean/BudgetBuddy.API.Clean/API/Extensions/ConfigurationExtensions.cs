using Azure.Identity;
using BudgetBuddy.API.Configuration;

namespace BudgetBuddy.API.Extensions;

/// <summary>
/// Extension methods for registering strongly-typed configuration options
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Registers typed configuration objects from appsettings.json
    /// </summary>
    public static void AddTypedConfigurations(this IServiceCollection services, WebApplicationBuilder builder)
    {
        // Rate Limiting Configuration
        services.AddOptions<RateLimitConfig>()
            .Bind(builder.Configuration.GetSection("RateLimit"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>
    /// Configures connection string source based on environment
    /// Development: Environment variables (ConnectionStrings__DefaultConnection, ConnectionStrings__Redis)
    /// Production: Azure Key Vault
    /// </summary>
    public static void AddConnectionStringConfiguration(this ConfigurationManager configuration, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            // Development: Use environment variables
            // Set environment variable: ConnectionStrings__DefaultConnection=your_connection_string
            var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrEmpty(envConnectionString))
            {
                configuration["ConnectionStrings:DefaultConnection"] = envConnectionString;
            }

            // Redis connection string from environment variable
            // NOTE: Development uses in-memory distributed cache by default (not Redis)
            // To test with real Redis in development: Set ConnectionStrings__Redis=localhost:6379
            // and install/run Redis locally (docker run -d -p 6379:6379 redis:alpine)
            var envRedisConnection = Environment.GetEnvironmentVariable("ConnectionStrings__Redis");
            if (!string.IsNullOrEmpty(envRedisConnection))
            {
                configuration["ConnectionStrings:Redis"] = envRedisConnection;
            }
            // No fallback: development uses in-memory distributed cache (see CachingExtensions).
            // To test with real Redis locally: set ConnectionStrings__Redis=localhost:6379
            // and run: docker run -d -p 6379:6379 redis:alpine
        }
        else if (environment.IsProduction())
        {
            // Production: Azure Key Vault is mandatory — fail fast if not configured
            var keyVaultUrl = configuration["KeyVault:Url"];
            if (string.IsNullOrEmpty(keyVaultUrl))
                throw new InvalidOperationException(
                    "KeyVault:Url is required in production. Configure it via environment variable or appsettings.Production.json.");

            configuration.AddAzureKeyVault(
                new Uri(keyVaultUrl),
                new DefaultAzureCredential());
        }
    }
}