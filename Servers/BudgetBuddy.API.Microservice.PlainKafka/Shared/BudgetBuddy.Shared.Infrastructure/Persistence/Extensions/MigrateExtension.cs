using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Shared.Infrastructure.Persistence.Extensions;

public static class MigrateExtension
{
    public static async Task MigrateDatabaseAsync(this IHost host)
        => await host.MigrateDatabaseAsync<AppDbContext>();

    /// <summary>
    /// Applies all pending EF Core migrations on startup (M-01).
    ///
    /// Rollback strategy:
    /// <list type="bullet">
    ///   <item><b>Forward-only migrations</b>: EF Core migrations are applied in order and are not
    ///         automatically reversible. Every migration must be backward-compatible with the previous
    ///         service version so a failed deployment can be rolled back by redeploying the old image
    ///         without reverting the schema.</item>
    ///   <item><b>Manual rollback</b>: run <c>dotnet ef database update &lt;PreviousMigrationName&gt;</c>
    ///         against the target database to revert to the last good migration. Requires the migration
    ///         assembly to be present and a valid connection string.</item>
    ///   <item><b>Emergency</b>: take a point-in-time restore from the PostgreSQL backup made before
    ///         the deployment. Recovery Point Objective (RPO) depends on backup schedule (default: daily).</item>
    ///   <item><b>Destructive changes</b> (column drops, renames): always done in two deployments —
    ///         (1) add new column + dual-write, (2) drop old column — to preserve rollback safety.</item>
    /// </list>
    /// </summary>
    public static async Task MigrateDatabaseAsync<TContext>(this IHost host)
        where TContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var db = services.GetRequiredService<TContext>();
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Database migration failed for {typeof(TContext).Name}.", ex);
        }
    }
}
