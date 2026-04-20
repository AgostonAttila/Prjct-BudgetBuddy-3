using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Shared.Infrastructure.Persistence.Extensions;

public static class MigrateExtension
{
    public static async Task MigrateDatabaseAsync(this IHost host)
        => await host.MigrateDatabaseAsync<AppDbContext>();

    public static async Task MigrateDatabaseAsync<TContext>(this IHost host)
        where TContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<TContext>>();
        try
        {
            var db = services.GetRequiredService<TContext>();
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed for {DbContext}", typeof(TContext).Name);
            throw;
        }
    }
}
