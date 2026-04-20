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

        // Database with PostgreSQL
        // NOTE: Using AddDbContext instead of AddDbContextPool due to scoped interceptors (RLS, AuditLog)
        // These interceptors require HttpContext (per-request) which is not compatible with pooling
        // Performance trade-off: ~20-30% slower, but security interceptors are critical
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var connectionStringProvider = serviceProvider.GetRequiredService<IConnectionStringProvider>();
            var connectionString = connectionStringProvider.GetDbConnectionString();

            var auditInterceptor = serviceProvider.GetRequiredService<AuditableEntityInterceptor>();
            var rlsInterceptor = serviceProvider.GetRequiredService<RowLevelSecurityInterceptor>();
            var auditLogInterceptor = serviceProvider.GetRequiredService<AuditLogInterceptor>();

            options.UseNpgsql(connectionString, sqlOptions =>
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
                .AddInterceptors(auditInterceptor, rlsInterceptor, auditLogInterceptor); // Add all interceptors
        });

        // ISeeder (demo data) is registered by the Host project to avoid circular dependencies
        // RoleSeeder and AdminUserSeeder are registered by AuthModule.RegisterServices()
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
                })
                .EnableThreadSafetyChecks(true)
                .AddInterceptors(auditInterceptor, rlsInterceptor, auditLogInterceptor, outboxInterceptor);
        });

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
            await seeder.Seed();
    }
}