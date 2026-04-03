using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;

/// <summary>
/// Spins up real PostgreSQL and Redis via Testcontainers.
/// Shared across all tests in a collection to avoid per-test container startup cost.
/// </summary>
public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("budgetbuddy_test")
        .WithUsername("postgres")
        .WithPassword("postgres_test")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder().Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                // Disable services that require external infrastructure in tests
                ["ClamAV:Enabled"] = "false",
                ["Email:Enabled"] = "false",
                ["BackgroundJobs:Enabled"] = "false",
                // JWT secret for integration tests — not a real secret, test-only
                ["Jwt:SecretKey"] = "BudgetBuddy-CI-Xk9mNpQ2rV8wL5jH3dFbY7sD1vC6uA4e1fGbYnZqPrKvMcR",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace DbContext to point to the test container database
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString(),
                    o => o.UseNodaTime()));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        // Run migrations against the test database
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
