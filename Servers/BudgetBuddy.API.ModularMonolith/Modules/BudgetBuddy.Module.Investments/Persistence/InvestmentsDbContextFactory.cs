using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BudgetBuddy.Module.Investments.Persistence;

public class InvestmentsDbContextFactory : IDesignTimeDbContextFactory<InvestmentsDbContext>
{
    public InvestmentsDbContext CreateDbContext(string[] args)
    {
        var config = BuildConfiguration();
        var connectionString = config["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings:DefaultConnection via user-secrets on the Host project.");

        var options = new DbContextOptionsBuilder<InvestmentsDbContext>()
            .UseNpgsql(connectionString, sql => sql.UseNodaTime())
            .Options;

        return new InvestmentsDbContext(options);
    }

    private static IConfiguration BuildConfiguration()
    {
        var userSecretsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "UserSecrets", "budgetbuddy-modular-monolith", "secrets.json");

        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile(userSecretsPath, optional: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
