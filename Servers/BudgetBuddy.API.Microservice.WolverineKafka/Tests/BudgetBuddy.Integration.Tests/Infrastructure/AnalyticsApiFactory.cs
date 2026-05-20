using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Messages.Contracts.Financial;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

public class AnalyticsApiFactory
    : WebApplicationFactory<BudgetBuddy.Service.Analytics.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("analytics_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private Respawner _respawner = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _ = Server;

        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["analytics"],
        });
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "test");
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = _postgres.GetConnectionString();

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(InvestmentsApiFactory.BuildTestConfig(connectionString)));

        builder.ConfigureServices(services =>
        {
            InvestmentsApiFactory.ReplaceConnectionStringProvider(services, connectionString);
            InvestmentsApiFactory.OverrideDataProtection(services);
            InvestmentsApiFactory.ReplaceSerilogLogger(services);
            InvestmentsApiFactory.ReplaceCurrencyServices(services);
            ReplaceFinancialServiceClient(services);
            InvestmentsApiFactory.ReplaceAuthentication(services);

            // Disable HybridCache TTL so tests never read stale cached data
            services.PostConfigure<HybridCacheOptions>(opts =>
                opts.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration      = TimeSpan.FromSeconds(1),
                    LocalCacheExpiration = TimeSpan.FromSeconds(1),
                });

        });
    }

    private static void ReplaceFinancialServiceClient(IServiceCollection services)
    {
        // FinancialServiceClient is registered as IPriceService by AddFinancialServiceClient.
        // Remove it so that InvestmentCalculationService falls back to PriceSnapshot table.
        var toRemove = services
            .Where(d => d.ServiceType == typeof(IPriceService))
            .ToList();
        foreach (var d in toRemove) { services.Remove(d); }

        services.AddScoped<IPriceService>(_ => null!);
    }
}
