using BudgetBuddy.Shared.Infrastructure.Persistence;
using BudgetBuddy.Shared.Infrastructure.Persistence.ConnectionStrings;
using BudgetBuddy.Shared.Infrastructure.Persistence.Interceptors;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Infrastructure.Persistence.Seeders;


namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class DatabaseExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Auditable Entity Interceptor
        services.AddSingleton<AuditableEntityInterceptor>();

        // Register Row-Level Security Interceptor (scoped - needs HttpContext)
        services.AddScoped<RowLevelSecurityInterceptor>();

        // Register Audit Log Interceptor (scoped - needs HttpContext)
        services.AddScoped<AuditLogInterceptor>();

        // Outbox: collector is scoped (per-request), interceptor is scoped (needs collector)
        services.AddScoped<IDomainEventCollector, DomainEventCollector>();
        services.AddScoped<OutboxInterceptor>();

        services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();

        // NOTE: AppDbContext is abstract — each service registers its own concrete DbContext via AddModuleDbContext<TContext>.
        // ISeeder (demo data) is registered by the Host project to avoid circular dependencies.
    }

    /// <summary>
    /// Registers a module-specific DbContext with the same connection string and interceptors as AppDbContext.
    /// Each module calls this from its RegisterServices() to own its own schema.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            var connectionStringProvider = serviceProvider.GetRequiredService<IConnectionStringProvider>();
            var connectionString = connectionStringProvider.GetDbConnectionString();

            var auditInterceptor = serviceProvider.GetRequiredService<AuditableEntityInterceptor>();
            var rlsInterceptor = serviceProvider.GetRequiredService<RowLevelSecurityInterceptor>();
            var auditLogInterceptor = serviceProvider.GetRequiredService<AuditLogInterceptor>();
            var outboxInterceptor = serviceProvider.GetRequiredService<OutboxInterceptor>();

            options.UseNpgsql(connectionString, sqlOptions =>
                {
                    sqlOptions.UseNodaTime();
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.MigrationsAssembly(typeof(TContext).Assembly.FullName);
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
                .EnableThreadSafetyChecks(true)
                .AddInterceptors(auditInterceptor, rlsInterceptor, auditLogInterceptor, outboxInterceptor);
        });

        return services;
    }

    /// <summary>
    /// Adds a health check for the given DbContext under the "database" name.
    /// Call this after AddModuleDbContext in each service's Program.cs.
    /// </summary>
    public static IServiceCollection AddDbHealthCheck<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddHealthChecks()
            .AddDbContextCheck<TContext>("database", tags: ["ready", "live"]);
        return services;
    }

    /// <summary>
    /// Registers a <see cref="ReadOnlyDbContext{TContext}"/> alongside the write context.
    /// Inject <c>ReadOnlyDbContext&lt;TContext&gt;</c> in query handlers for zero-overhead
    /// no-tracking reads on the CQRS query side.
    /// Item 10: Read-only DbContext for CQRS Query side
    /// </summary>
    public static IServiceCollection AddReadOnlyDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<ReadOnlyDbContext<TContext>>(sp =>
            new ReadOnlyDbContext<TContext>(sp.GetRequiredService<TContext>()));
        return services;
    }

    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        // Execute all registered ISeeder instances in registration order:
        // 1. RoleSeeder (registered by AuthModule)
        // 2. AdminUserSeeder (registered by AuthModule)
        // 3. Seeder / demo data (registered by Host)
        var seeders = scope.ServiceProvider.GetServices<ISeeder>();
        foreach (var seeder in seeders)
        {
            await seeder.Seed();
        }
    }
}