using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BudgetBuddy.Service.Analytics.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (dotnet ef migrations add/remove/update).
/// Uses the development connection string; not used at runtime.
/// </summary>
public class AnalyticsDbContextFactory : IDesignTimeDbContextFactory<AnalyticsDbContext>
{
    public AnalyticsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AnalyticsDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5438;Database=analytics;Username=postgres;Password=postgres",
            o => o.UseNodaTime());
        return new AnalyticsDbContext(optionsBuilder.Options);
    }
}
