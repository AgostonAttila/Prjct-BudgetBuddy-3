using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.ConnectionStrings;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.Interceptors;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.Seeders;


namespace BudgetBuddy.API.VSA.Common.Extensions;

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

        services.AddScoped<ISeeder, Seeder>();
        services.AddScoped<RoleSeeder>();
        services.AddScoped<AdminUserSeeder>();
    }

    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        // 1. Seed roles first (required for admin user)
        var roleSeeder = scope.ServiceProvider.GetRequiredService<RoleSeeder>();
        await roleSeeder.Seed();

        // 2. Seed admin user
        var adminSeeder = scope.ServiceProvider.GetRequiredService<AdminUserSeeder>();
        await adminSeeder.Seed();

        // 3. Seed demo data (currencies, categories, transactions, etc.)
        var dataSeeder = scope.ServiceProvider.GetRequiredService<ISeeder>();
        await dataSeeder.Seed();
    }
}