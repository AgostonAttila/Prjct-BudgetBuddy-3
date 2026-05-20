using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Service.Investments.Domain;
using BudgetBuddy.Service.Investments.Persistence;
using BudgetBuddy.Service.Transactions.Persistence;
using BudgetBuddy.Shared.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Wolverine;
using Xunit;
using AnalyticsAccountSnapshot  = BudgetBuddy.Service.Analytics.ReadModels.AccountSnapshot;
using AnalyticsCategorySnapshot  = BudgetBuddy.Service.Analytics.ReadModels.CategorySnapshot;
using InvestmentsAccountSnapshot = BudgetBuddy.Service.Investments.ReadModels.AccountSnapshot;
using TransactionsAccountSnapshot = BudgetBuddy.Service.Transactions.ReadModels.AccountSnapshot;
using TransactionsCategorySnapshot = BudgetBuddy.Service.Transactions.ReadModels.CategorySnapshot;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

/// <summary>
/// Combined fixture for E2E tests.
///
/// Owns three independent WebApplicationFactories (one per service), each with its own
/// Testcontainers PostgreSQL instance.
///
/// Why direct seeding instead of HTTP POST for create:
///   The EF Core outbox (IDbContextOutbox) requires PostgreSQL-backed Wolverine message
///   persistence.  Enabling it for tests triggers a .NET 10 WebApplicationFactory bug
///   where PersistMessagesWithPostgresql's durability-agent registration causes
///   ConfigureHostBuilder(IHostBuilder) to access a disposed IServiceProvider.
///   Direct entity seeding bypasses the outbox entirely and lets us focus on testing
///   the Wolverine handler pipeline — which is the core goal.
///
/// Wolverine approach: Kafka is disabled in tests.  DispatchToAnalyticsAsync invokes
/// a Wolverine handler synchronously via IMessageBus.InvokeAsync — this runs the full
/// local handler chain (SchemaVersionMiddleware + handler) without requiring Kafka.
///
/// Known limitation: the .NET 10 WebApplicationFactory calls ConfigureHostBuilder(IHostBuilder)
///   on a code path that accesses an already-disposed IServiceProvider when
///   PersistMessagesWithPostgresql registers its durability agent (ObjectDisposedException).
///   If WolverineFx fixes this upstream, remove DisableAllWolverineMessagePersistence()
///   from test factories and restore HTTP-based seeding for create operations.
/// </summary>
public class E2ETestFixture : IAsyncLifetime
{
    public TransactionsApiFactory Transactions { get; } = new();
    public InvestmentsApiFactory  Investments  { get; } = new();
    public AnalyticsApiFactory    Analytics    { get; } = new();

    public async Task InitializeAsync()
    {
        // Must be sequential — multiple concurrent WebApplicationFactory inits trigger
        // the Serilog ReloadableLogger static race (see TestAssemblyInfo.cs).
        await Transactions.InitializeAsync();
        await Investments.InitializeAsync();
        await Analytics.InitializeAsync();

        await SeedAllAsync();
    }

    public async Task DisposeAsync()
    {
        await Analytics.DisposeAsync();
        await Investments.DisposeAsync();
        await Transactions.DisposeAsync();
    }

    /// <summary>
    /// Resets all three databases and re-seeds reference data.
    /// Call this in IAsyncLifetime.InitializeAsync of each test class.
    /// </summary>
    public async Task ResetAllAsync()
    {
        await Transactions.ResetDatabaseAsync();
        await Investments.ResetDatabaseAsync();
        await Analytics.ResetDatabaseAsync();
        await SeedAllAsync();
    }

    /// <summary>
    /// Dispatches a message through the Analytics service's Wolverine handler pipeline.
    /// IMessageBus.InvokeAsync is synchronous in-process: no Kafka, no transport routing —
    /// the handler (and middleware including SchemaVersionMiddleware) runs and completes
    /// before this method returns.
    /// </summary>
    public async Task DispatchToAnalyticsAsync<T>(T message) where T : notnull
    {
        await using var scope = Analytics.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.InvokeAsync(message);
    }

    /// <summary>
    /// Manually invalidates the Analytics HybridCache for a given user.
    /// Required after dispatching InvestmentCreatedEvent because InvestmentEventsHandler
    /// does not call IUserCacheInvalidator — the dashboard cache would otherwise serve
    /// stale data for the remaining 5-minute TTL (set by DashboardService explicitly).
    /// </summary>
    public async Task InvalidateAnalyticsCacheAsync(string userId)
    {
        await using var scope = Analytics.Services.CreateAsyncScope();
        var invalidator = scope.ServiceProvider.GetRequiredService<IUserCacheInvalidator>();
        await invalidator.InvalidateAsync(userId);
    }

    // ── Seeding ────────────────────────────────────────────────────────────────

    private async Task SeedAllAsync()
    {
        await SeedTransactionsRefDataAsync();
        await SeedInvestmentsRefDataAsync();
        await SeedAnalyticsRefDataAsync();
    }

    /// <summary>
    /// Seeds account + category snapshots into the Transactions DB.
    /// These are needed so GET /api/transactions works and category names resolve.
    /// </summary>
    private async Task SeedTransactionsRefDataAsync()
    {
        using var scope = Transactions.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

        db.AccountSnapshots.Add(new TransactionsAccountSnapshot
        {
            AccountId = TestDataSeeder.Account1Id,
            UserId    = TestDataSeeder.TestUserId,
            Name      = "USD Savings",
            Currency  = TestDataSeeder.Account1Currency,
            IsActive  = true,
            Version   = 1,
            SyncedAt  = DateTime.UtcNow,
        });

        db.CategorySnapshots.AddRange(
            new TransactionsCategorySnapshot
            {
                CategoryId = TestDataSeeder.SalaryCategory,
                UserId     = TestDataSeeder.TestUserId,
                Name       = "Salary",
                SyncedAt   = DateTime.UtcNow,
            },
            new TransactionsCategorySnapshot
            {
                CategoryId = TestDataSeeder.FoodCategory,
                UserId     = TestDataSeeder.TestUserId,
                Name       = "Food",
                SyncedAt   = DateTime.UtcNow,
            }
        );

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds account snapshots + an AAPL price snapshot into the Investments DB.
    /// AccountSnapshots → PortfolioService uses these to compute TotalAccountBalance.
    /// PriceSnapshot    → InvestmentCalculationService falls back to this when
    ///                    IPriceService is null (as replaced by InvestmentsApiFactory).
    /// </summary>
    private async Task SeedInvestmentsRefDataAsync()
    {
        using var scope = Investments.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestmentsDbContext>();

        var today = LocalDate.FromDateTime(DateTime.UtcNow);

        db.AccountSnapshots.AddRange(
            new InvestmentsAccountSnapshot
            {
                AccountId = TestDataSeeder.Account1Id,
                UserId    = TestDataSeeder.TestUserId,
                Name      = "USD Savings",
                Currency  = TestDataSeeder.Account1Currency,
                Balance   = TestDataSeeder.Account1Balance,
                IsActive  = true,
                Version   = 1,
                SyncedAt  = DateTime.UtcNow,
            },
            new InvestmentsAccountSnapshot
            {
                AccountId = TestDataSeeder.Account2Id,
                UserId    = TestDataSeeder.TestUserId,
                Name      = "HUF Savings",
                Currency  = TestDataSeeder.Account2Currency,
                Balance   = TestDataSeeder.Account2Balance,
                IsActive  = true,
                Version   = 1,
                SyncedAt  = DateTime.UtcNow,
            }
        );

        db.PriceSnapshots.Add(new PriceSnapshot
        {
            Symbol     = "AAPL",
            Date       = today,
            ClosePrice = TestDataSeeder.AaplCurrentPrice,   // 175 USD
            Currency   = "USD",
            Source     = "test",
            CreatedAt  = SystemClock.Instance.GetCurrentInstant(),
        });

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds account + category snapshots and an AAPL price snapshot into the Analytics DB.
    ///
    /// NOT seeded here (populated by Wolverine handlers during the test):
    ///   TransactionReadModel  → TransactionEventsHandler.Handle(TransactionCreatedEvent)
    ///   InvestmentReadModel   → InvestmentEventsHandler.Handle(InvestmentCreatedEvent)
    /// </summary>
    private async Task SeedAnalyticsRefDataAsync()
    {
        using var scope = Analytics.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();

        var today = LocalDate.FromDateTime(DateTime.UtcNow);

        db.AccountSnapshots.AddRange(
            new AnalyticsAccountSnapshot
            {
                AccountId = TestDataSeeder.Account1Id,
                UserId    = TestDataSeeder.TestUserId,
                Name      = "USD Savings",
                Currency  = TestDataSeeder.Account1Currency,
                Balance   = TestDataSeeder.Account1Balance,
                IsActive  = true,
                Version   = 1,
                SyncedAt  = DateTime.UtcNow,
            },
            new AnalyticsAccountSnapshot
            {
                AccountId = TestDataSeeder.Account2Id,
                UserId    = TestDataSeeder.TestUserId,
                Name      = "HUF Savings",
                Currency  = TestDataSeeder.Account2Currency,
                Balance   = TestDataSeeder.Account2Balance,
                IsActive  = true,
                Version   = 1,
                SyncedAt  = DateTime.UtcNow,
            }
        );

        db.CategorySnapshots.AddRange(
            new AnalyticsCategorySnapshot
            {
                CategoryId = TestDataSeeder.SalaryCategory,
                UserId     = TestDataSeeder.TestUserId,
                Name       = "Salary",
                SyncedAt   = DateTime.UtcNow,
            },
            new AnalyticsCategorySnapshot
            {
                CategoryId = TestDataSeeder.FoodCategory,
                UserId     = TestDataSeeder.TestUserId,
                Name       = "Food",
                SyncedAt   = DateTime.UtcNow,
            }
        );

        db.PriceSnapshots.Add(new PriceSnapshotReadModel
        {
            Symbol   = "AAPL",
            Date     = today,
            PriceUsd = TestDataSeeder.AaplCurrentPrice,   // 175 USD
            SyncedAt = SystemClock.Instance.GetCurrentInstant(),
        });

        await db.SaveChangesAsync();
    }
}
