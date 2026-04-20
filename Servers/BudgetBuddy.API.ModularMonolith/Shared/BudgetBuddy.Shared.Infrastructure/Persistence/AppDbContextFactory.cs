using BudgetBuddy.Shared.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BudgetBuddy.Shared.Infrastructure.Persistence;

/// <summary>
/// Design-time DbContext factory for EF Core migrations (AppDbContext = Identity + Auth only).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Load user secrets by known ID (Host project owns the UserSecretsId)
        const string userSecretsId = "budgetbuddy-modular-monolith";
        var userSecretsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "UserSecrets", userSecretsId, "secrets.json");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile(userSecretsPath, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found. " +
                "Use: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<conn>\" " +
                "--project Host/BudgetBuddy.API.ModularMonolith");

        var auditInterceptor = new AuditableEntityInterceptor(NodaTime.SystemClock.Instance);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, sql =>
            {
                sql.UseNodaTime();
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            })
            .AddInterceptors(auditInterceptor)
            .Options;

        return new AppDbContext(options);
    }
}
