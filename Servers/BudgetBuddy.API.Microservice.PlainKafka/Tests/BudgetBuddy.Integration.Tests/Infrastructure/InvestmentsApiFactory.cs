using System.Net.Http.Headers;
using BudgetBuddy.Service.Investments.Financial;
using BudgetBuddy.Service.Investments.Financial.Providers;
using BudgetBuddy.Shared.Infrastructure.Financial;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Xunit;
using BudgetBuddy.Service.Investments.Messaging.Consumers;
using BudgetBuddy.Service.Investments.Persistence;
using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.ConnectionStrings;
using BudgetBuddy.Shared.Messages.Contracts.Financial;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

public class InvestmentsApiFactory
    : WebApplicationFactory<BudgetBuddy.Service.Investments.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("investments_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private Respawner _respawner = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Force host creation so migrations run
        _ = Server;

        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["investments"],
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
            cfg.AddInMemoryCollection(BuildTestConfig(connectionString)));

        builder.ConfigureServices(services =>
        {
            ReplaceConnectionStringProvider(services, connectionString);
            OverrideDataProtection(services);
            ReplaceSerilogLogger(services);
            ReplaceCurrencyServices(services);
            RemovePriceServices(services);
            ReplaceAuthentication(services);
            RemoveHostedService<AccountChangelogConsumer>(services);
            RemoveHostedService<KafkaTopicValidatorService>(services);
        });
    }

    internal static Dictionary<string, string?> BuildTestConfig(string connectionString) => new()
    {
        ["ConnectionStrings:DefaultConnection"] = connectionString,
        ["Keycloak:Authority"]    = "https://test-authority",
        ["Keycloak:Audience"]     = "test-audience",
        ["Keycloak:RequireHttps"] = "false",
        ["Kafka:BootstrapServers"] = "localhost:9999",
        ["BackgroundJobs:Enabled"] = "false",
        ["FinancialService:BaseUrl"] = "http://localhost:1",
        ["PriceService:YahooFinanceBaseUrl"] = "http://localhost:1",
        ["PriceService:CoinGeckoBaseUrl"] = "http://localhost:1",
        ["RateLimitConfig:Fixed:PermitLimit"] = "10000",
        ["RateLimitConfig:Fixed:WindowMinutes"] = "1",
        ["RateLimitConfig:Fixed:QueueLimit"] = "0",
        ["RateLimitConfig:FixedByIp:PermitLimit"] = "10000",
        ["RateLimitConfig:FixedByIp:WindowMinutes"] = "1",
        ["RateLimitConfig:FixedByIp:QueueLimit"] = "0",
        ["RateLimitConfig:Auth:TokenLimit"] = "1000",
        ["RateLimitConfig:Auth:ReplenishmentPeriodMinutes"] = "1",
        ["RateLimitConfig:Auth:TokensPerPeriod"] = "100",
        ["RateLimitConfig:Global:PermitLimit"] = "100000",
        ["RateLimitConfig:Global:WindowMinutes"] = "1",
        ["RateLimitConfig:Global:QueueLimit"] = "0",
        ["RateLimitConfig:Api:PermitLimit"] = "10000",
        ["RateLimitConfig:Api:WindowMinutes"] = "1",
        ["RateLimitConfig:Api:SegmentsPerWindow"] = "2",
        ["RateLimitConfig:Api:QueueLimit"] = "0",
        ["RateLimitConfig:Refresh:PermitLimit"] = "1000",
        ["RateLimitConfig:Refresh:WindowMinutes"] = "1",
        ["RateLimitConfig:Refresh:QueueLimit"] = "0",
        ["RateLimitConfig:PerUser:PermitLimit"] = "10000",
        ["RateLimitConfig:PerUser:WindowMinutes"] = "1",
        ["RateLimitConfig:PerUser:QueueLimit"] = "0",
    };

    internal static void ReplaceSerilogLogger(IServiceCollection services)
    {
        // xUnit runs test collections in parallel → multiple WebApplicationFactories set the shared
        // static Log.Logger = new ReloadableLogger() concurrently → Serilog.Freeze() races and throws
        // "The logger is already frozen." Fix: replace the Serilog.ILogger singleton that calls Freeze()
        // with a pre-built concrete logger that never touches the static bootstrap logger.
        var toRemove = services.Where(d => d.ServiceType == typeof(ILogger)).ToList();
        foreach (var d in toRemove) { services.Remove(d); }
        services.AddSingleton<ILogger>(
            _ => new LoggerConfiguration().MinimumLevel.Warning().WriteTo.Console().CreateLogger());
    }

    internal static void ReplaceConnectionStringProvider(IServiceCollection services, string connectionString)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConnectionStringProvider));
        if (descriptor != null) { services.Remove(descriptor); }
        services.AddSingleton<IConnectionStringProvider>(_ => new TestConnectionStringProvider(connectionString));
    }

    internal static void OverrideDataProtection(IServiceCollection services)
    {
        // UseEphemeralDataProtectionProvider overwrites the PersistKeysToFileSystem registration —
        // avoids /tmp/dp-keys path issues on Windows and in CI containers.
        services.AddDataProtection()
            .SetApplicationName("BudgetBuddy")
            .UseEphemeralDataProtectionProvider();
    }

    internal static void ReplaceCurrencyServices(IServiceCollection services)
    {
        // Remove FrankfurterCurrencyConversionService registrations
        var toRemove = services
            .Where(d => d.ServiceType == typeof(ICurrencyConversionService)
                     || d.ServiceType == typeof(IFxHistoricalProvider)
                     || d.ImplementationType == typeof(FrankfurterCurrencyConversionService))
            .ToList();
        foreach (var d in toRemove) { services.Remove(d); }

        services.AddSingleton<ICurrencyConversionService, StubCurrencyConversionService>();

        // StubCurrencyConversionService does not implement IFxHistoricalProvider.
        // Register a substitute that returns consistent stub rates for any date range.
        var fxStub = Substitute.For<IFxHistoricalProvider>();
        fxStub.GetDailyRatesAsync(default, default, Arg.Any<string>(), Arg.Any<CancellationToken>())
              .ReturnsForAnyArgs(callInfo =>
              {
                  var from = callInfo.ArgAt<LocalDate>(0);
                  var to   = callInfo.ArgAt<LocalDate>(1);
                  var result = new Dictionary<LocalDate, Dictionary<string, decimal>>();
                  for (var d = from; d <= to; d = d.PlusDays(1))
                  {
                      result[d] = new Dictionary<string, decimal>
                      {
                          ["USD"] = 1.0m,
                          ["HUF"] = 370.50m,
                          ["EUR"] = 0.92m,
                          ["GBP"] = 0.79m,
                      };
                  }
                  return Task.FromResult(result);
              });
        services.AddSingleton(fxStub);
    }

    internal static void RemovePriceServices(IServiceCollection services)
    {
        // Remove live price service registrations (Yahoo Finance, CoinGecko)
        var toRemove = services
            .Where(d => d.ServiceType == typeof(IPriceService)
                     || d.ServiceType == typeof(IPriceProvider))
            .ToList();
        foreach (var d in toRemove) { services.Remove(d); }

        // IPriceService? is nullable in InvestmentCalculationService — null triggers fallback to PriceSnapshot
        services.AddSingleton<IPriceService>(_ => null!);
    }

    internal static void ReplaceAuthentication(IServiceCollection services)
    {
        // Override default auth scheme to use TestAuthHandler instead of JwtBearer
        services.PostConfigure<AuthenticationOptions>(opts =>
        {
            opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
            opts.DefaultChallengeScheme    = TestAuthHandler.SchemeName;
            opts.DefaultScheme             = TestAuthHandler.SchemeName;
        });
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
    }

    internal static void RemoveHostedService<T>(IServiceCollection services) where T : class
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(T))
            .ToList();
        foreach (var d in descriptors) { services.Remove(d); }
    }
}
