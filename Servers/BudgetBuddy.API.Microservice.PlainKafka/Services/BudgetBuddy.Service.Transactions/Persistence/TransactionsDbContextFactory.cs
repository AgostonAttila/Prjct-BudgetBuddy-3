using BudgetBuddy.Shared.Infrastructure.Security.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BudgetBuddy.Service.Transactions.Persistence;

public class TransactionsDbContextFactory : IDesignTimeDbContextFactory<TransactionsDbContext>
{
    public TransactionsDbContext CreateDbContext(string[] args)
    {
        var config = BuildConfiguration();
        var connectionString = config["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__DefaultConnection environment variable before running migrations.");

        var options = new DbContextOptionsBuilder<TransactionsDbContext>()
            .UseNpgsql(connectionString, sql => sql.UseNodaTime())
            .Options;

        // Design-time only: encryption is not exercised during migrations
        return new TransactionsDbContext(options, new DesignTimeEncryptionService());
    }

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

    /// <summary>No-op encryption for EF Core design-time tooling.</summary>
    private sealed class DesignTimeEncryptionService : IEncryptionService
    {
        public string? Encrypt(string? plainText, string purpose) => plainText;
        public string? Decrypt(string? encryptedText, string purpose) => encryptedText;
    }
}
