using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.ConnectionStrings;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.Interceptors;
using BudgetBuddy.API.VSA.Common.Infrastructure.Security.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;

/// <summary>
/// Design-time DbContext factory for EF Core migrations
/// This allows migrations to work with DbContextPool and scoped interceptors
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<AppDbContextFactory>(optional: true)
            .Build();

        // Get connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found. " +
                "Set it via user secrets: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<your-connection-string>\"");
        }

        // Create options builder
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // DESIGN-TIME: Configure without scoped interceptors (they require HttpContext)
        // Only add the singleton AuditableEntityInterceptor for timestamp management
        var auditInterceptor = new AuditableEntityInterceptor(NodaTime.SystemClock.Instance);

        optionsBuilder.UseNpgsql(connectionString, sqlOptions =>
            {
                sqlOptions.UseNodaTime();
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                sqlOptions.CommandTimeout(30);
                sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            })
            .EnableThreadSafetyChecks(true)
            .AddInterceptors(auditInterceptor); // Only singleton interceptor for design-time

        return new AppDbContext(optionsBuilder.Options, new NoOpEncryptionService());
    }
}
