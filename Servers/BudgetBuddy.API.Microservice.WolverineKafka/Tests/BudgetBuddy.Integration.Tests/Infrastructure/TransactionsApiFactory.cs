using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Wolverine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using BudgetBuddy.Service.Transactions.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

public class TransactionsApiFactory
    : WebApplicationFactory<BudgetBuddy.Service.Transactions.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("transactions_test")
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
            SchemasToInclude = ["transactions"],
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
            InvestmentsApiFactory.ReplaceAuthentication(services);
            services.DisableAllExternalWolverineTransports();
            services.DisableAllWolverineMessagePersistence();
        });
    }
}
