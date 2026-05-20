using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Service.Analytics.Features.Dashboard.GetDashboard;
using BudgetBuddy.Service.Analytics.Features.Reports.GetIncomeVsExpense;
using BudgetBuddy.Service.Analytics.Features.Reports.GetInvestmentPerformance;
using BudgetBuddy.Service.Investments.Domain;
using BudgetBuddy.Service.Investments.Features.GetPortfolioValue;
using BudgetBuddy.Service.Investments.Persistence;
using BudgetBuddy.Service.Transactions.Domain;
using BudgetBuddy.Service.Transactions.Persistence;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Events.Investments;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Messaging;

/// <summary>
/// End-to-end test that verifies the full Wolverine handler pipeline between services —
/// without Kafka, by using IMessageBus.InvokeAsync to synchronously invoke Analytics handlers.
///
/// Why direct entity seeding instead of HTTP POST:
///   IDbContextOutbox.Enroll requires PostgreSQL-backed Wolverine persistence.
///   Enabling this for tests triggers a .NET 10 WebApplicationFactory bug (the
///   PersistMessagesWithPostgresql durability-agent registration takes the old
///   IHostBuilder code path → ObjectDisposedException on a disposed IServiceProvider).
///   Direct seeding avoids the outbox; IMessageBus.InvokeAsync still exercises the
///   complete Wolverine handler pipeline (SchemaVersionMiddleware + handler logic).
///
/// What is proven by this test:
///   1. TransactionCreatedEvent is correctly structured and accepted by the handler
///   2. TransactionEventsHandler runs via Wolverine → TransactionReadModel persisted
///   3. Analytics dashboard reflects the transaction (account-adjusted balance)
///   4. Analytics income-vs-expense report reflects the transaction
///   5. InvestmentCreatedEvent is correctly structured and accepted by the handler
///   6. InvestmentEventsHandler runs via Wolverine → InvestmentReadModel persisted
///   7. GET /api/portfolio/value uses PriceSnapshot prices ("arfolyam") correctly
///   8. Analytics investment-performance report reflects the investment + PriceSnapshot
///   9. Dashboard TotalBalance includes investment value after event dispatch
/// </summary>
[Collection(nameof(E2ETestCollection))]
public class WolverineFlowE2ETests(E2ETestFixture fixture) : IAsyncLifetime
{
    // E2E-specific values — distinct from TestDataSeeder amounts so assertions are unambiguous
    private static readonly Guid   TxId          = Guid.NewGuid();
    private static readonly Guid   InvId         = Guid.NewGuid();
    private const decimal          IncomeAmount   = 5_000m;     // USD income
    private const decimal          AaplQty        = 5m;         // shares purchased
    private const decimal          AaplPurchase   = TestDataSeeder.AaplPurchasePrice; // 150 USD
    private const decimal          AaplCurrent    = TestDataSeeder.AaplCurrentPrice;  // 175 USD (PriceSnapshot)
    private static readonly LocalDate TxDate       = new(2026, 5, 10);
    private static readonly LocalDate PurchaseDate = new(2026, 4,  1);

    // Precomputed expected values
    // Account1 adjusted: 1000 (opening) + 5000 (income) = 6000 USD
    // Account2: 370500 HUF / 370.50 = 1000 USD → total accounts: 7000 USD
    private const decimal ExpectedAccountsTotal   = 7_000m;
    // AAPL: 5 × 175 (PriceSnapshot) = 875 USD
    private const decimal ExpectedInvestmentValue = AaplQty * AaplCurrent;                        // 875
    private const decimal ExpectedDashboardTotal  = ExpectedAccountsTotal + ExpectedInvestmentValue; // 7875
    // Portfolio (Investments service): Account1 1000 + Account2 1000 (no tx-adjustment) = 2000
    private const decimal ExpectedPortfolioAccounts = TestDataSeeder.ExpectedAccountBalanceUsd;   // 2000
    private const decimal ExpectedPortfolioTotal    = ExpectedPortfolioAccounts + ExpectedInvestmentValue; // 2875

    private HttpClient _invClient = null!;
    private HttpClient _anlClient = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAllAsync();
        _invClient = fixture.Investments.CreateAuthenticatedClient();
        _anlClient = fixture.Analytics.CreateAuthenticatedClient();
    }

    public Task DisposeAsync()
    {
        _invClient.Dispose();
        _anlClient.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FullFlow_TransactionAndInvestment_WolverineHandlersUpdateAnalytics()
    {
        // ────────────────────────────────────────────────────────────────────────
        // STEP 1: Seed a transaction into the Transactions DB
        //
        // IDbContextOutbox cannot be used in WebApplicationFactory tests because
        // PersistMessagesWithPostgresql triggers the .NET 10 IHostBuilder bug.
        // We seed the entity directly and manually construct the corresponding event.
        // ────────────────────────────────────────────────────────────────────────

        await SeedTransactionAsync();

        // ────────────────────────────────────────────────────────────────────────
        // STEP 2: Dispatch TransactionCreatedEvent to Analytics via Wolverine
        //
        // In production the Transactions outbox publishes this event to Kafka.
        // IMessageBus.InvokeAsync routes through the full handler pipeline
        // (SchemaVersionMiddleware validates [SupportedSchemaVersions("1")],
        //  then TransactionEventsHandler.Handle creates the TransactionReadModel).
        // ────────────────────────────────────────────────────────────────────────

        await fixture.DispatchToAnalyticsAsync(new TransactionCreatedEvent
        {
            TransactionId   = TxId,
            UserId          = TestDataSeeder.TestUserId,
            AccountId       = TestDataSeeder.Account1Id,
            CategoryId      = TestDataSeeder.SalaryCategory,
            Amount          = IncomeAmount,
            CurrencyCode    = "USD",
            TransactionType = TransactionType.Income,
            TransactionDate = TxDate,
            IsTransfer      = false,
        });

        // ────────────────────────────────────────────────────────────────────────
        // STEP 3: Analytics dashboard reflects the transaction
        // ────────────────────────────────────────────────────────────────────────

        var dashResp1 = await _anlClient.GetAsync("/api/dashboard?displayCurrency=USD");
        dashResp1.StatusCode.Should().Be(HttpStatusCode.OK);

        var dash1 = await dashResp1.Content
            .ReadFromJsonAsync<GetDashboardResponse>(TestJsonOptions.Default);
        dash1.Should().NotBeNull();
        dash1!.AccountsSummary.AccountCount.Should().Be(2);

        // Account1 adjusted: opening 1000 + income 5000 = 6000; Account2: 1000 → total 7000
        // No investments dispatched yet → investment component = 0
        dash1.AccountsSummary.TotalBalance.Should().BeApproximately(ExpectedAccountsTotal, 1m,
            "dashboard must include the income transaction in account-adjusted balance");

        // ────────────────────────────────────────────────────────────────────────
        // STEP 4: Analytics income-vs-expense report reflects the transaction
        // ────────────────────────────────────────────────────────────────────────

        var today = LocalDate.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", null);
        var reportResp = await _anlClient.GetAsync(
            $"/api/reports/income-vs-expense?startDate=2026-01-01&endDate={today}&displayCurrency=USD");
        reportResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var report = await reportResp.Content
            .ReadFromJsonAsync<IncomeVsExpenseResponse>(TestJsonOptions.Default);
        report.Should().NotBeNull();
        report!.TotalIncome.Should().BeApproximately(IncomeAmount, 0.02m,
            "income-vs-expense report must include the dispatched TransactionCreatedEvent");
        report.TotalExpense.Should().Be(0m);
        report.NetIncome.Should().BeApproximately(IncomeAmount, 0.02m);
        report.IncomeTransactionCount.Should().Be(1);
        report.MonthlyData.Should().Contain(m => m.Year == 2026 && m.Month == 5 && m.Income > 0,
            "May 2026 monthly breakdown must show the income");

        // ────────────────────────────────────────────────────────────────────────
        // STEP 5: Seed an investment into the Investments DB
        // ────────────────────────────────────────────────────────────────────────

        await SeedInvestmentAsync();

        // ────────────────────────────────────────────────────────────────────────
        // STEP 6: Portfolio value uses PriceSnapshot ("arfolyam" lookup)
        //
        // InvestmentsApiFactory replaces IPriceService with null so
        // InvestmentCalculationService falls back to the PriceSnapshot table
        // (seeded with AAPL = 175 USD in E2ETestFixture.SeedInvestmentsRefDataAsync).
        // ────────────────────────────────────────────────────────────────────────

        var portfolioResp = await _invClient.GetAsync("/api/portfolio/value?currency=USD");
        portfolioResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var portfolio = await portfolioResp.Content
            .ReadFromJsonAsync<PortfolioValueResponse>(TestJsonOptions.Default);
        portfolio.Should().NotBeNull();
        portfolio!.Currency.Should().Be("USD");

        // AAPL: 5 shares × 175 USD (PriceSnapshot) = 875 USD
        portfolio.TotalInvestmentValue.Should().BeApproximately(ExpectedInvestmentValue, 0.02m,
            "portfolio investment value must reflect the AAPL PriceSnapshot price");

        // Account1 (1000 USD) + Account2 (370500 HUF / 370.50 = 1000 USD) = 2000 USD
        portfolio.TotalAccountBalance.Should().BeApproximately(ExpectedPortfolioAccounts, 0.02m);
        portfolio.TotalPortfolioValue.Should().BeApproximately(ExpectedPortfolioTotal, 0.02m);
        portfolio.InvestmentBreakdown.Should().Contain(i => i.Symbol == "AAPL");

        // ────────────────────────────────────────────────────────────────────────
        // STEP 7: Dispatch InvestmentCreatedEvent to Analytics via Wolverine
        // ────────────────────────────────────────────────────────────────────────

        await fixture.DispatchToAnalyticsAsync(new InvestmentCreatedEvent
        {
            InvestmentId  = InvId,
            UserId        = TestDataSeeder.TestUserId,
            Symbol        = "AAPL",
            Name          = "Apple Inc.",
            Type          = InvestmentType.Stock,
            Quantity      = AaplQty,
            PurchasePrice = AaplPurchase,
            CurrencyCode  = "USD",
            PurchaseDate  = PurchaseDate,
        });

        // ────────────────────────────────────────────────────────────────────────
        // STEP 8: Analytics investment-performance report reflects the investment
        // ────────────────────────────────────────────────────────────────────────

        var perfResp = await _anlClient.GetAsync(
            "/api/reports/investment-performance?displayCurrency=USD");
        perfResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var perf = await perfResp.Content
            .ReadFromJsonAsync<InvestmentPerformanceResponse>(TestJsonOptions.Default);
        perf.Should().NotBeNull();
        perf!.Investments.Should().HaveCount(1,
            "exactly one InvestmentCreatedEvent was dispatched to Analytics");

        var aapl = perf.Investments.Single(i => i.Symbol == "AAPL");
        aapl.Quantity.Should().Be(AaplQty);

        // Current value from PriceSnapshotReadModel (seeded 175 USD): 5 × 175 = 875 USD
        perf.CurrentValue.Should().BeApproximately(ExpectedInvestmentValue, 0.02m,
            "investment-performance must use PriceSnapshotReadModel from Analytics DB");
        aapl.GainLoss.Should().BeGreaterThan(0m,
            "AAPL current price (175) > purchase price (150) → positive gain");
        aapl.GainLossPercentage.Should().BeGreaterThan(0m);

        // ────────────────────────────────────────────────────────────────────────
        // STEP 9: Dashboard now includes investment value
        //
        // InvestmentEventsHandler does NOT call IUserCacheInvalidator.InvalidateAsync
        // (unlike TransactionEventsHandler), so the dashboard HybridCache entry
        // (TTL = 5 min, set explicitly by DashboardService) would serve stale data.
        // We manually invalidate via IUserCacheInvalidator before re-querying.
        // ────────────────────────────────────────────────────────────────────────

        await fixture.InvalidateAnalyticsCacheAsync(TestDataSeeder.TestUserId);

        var dashResp2 = await _anlClient.GetAsync("/api/dashboard?displayCurrency=USD");
        var dash2 = await dashResp2.Content
            .ReadFromJsonAsync<GetDashboardResponse>(TestJsonOptions.Default);

        // Accounts (7000) + AAPL investments (875) = 7875 USD
        dash2!.AccountsSummary.TotalBalance.Should().BeApproximately(ExpectedDashboardTotal, 1m,
            "dashboard must add investment value after InvestmentCreatedEvent is processed");
    }

    // ── Seeding helpers ────────────────────────────────────────────────────────

    private async Task SeedTransactionAsync()
    {
        using var scope = fixture.Transactions.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

        db.Transactions.Add(new Transaction
        {
            Id              = TxId,
            UserId          = TestDataSeeder.TestUserId,
            AccountId       = TestDataSeeder.Account1Id,
            CategoryId      = TestDataSeeder.SalaryCategory,
            Amount          = IncomeAmount,
            CurrencyCode    = "USD",
            TransactionType = TransactionType.Income,
            PaymentType     = PaymentType.BankTransfer,
            TransactionDate = TxDate,
            IsTransfer      = false,
            IsHidden        = false,
        });

        await db.SaveChangesAsync();
    }

    private async Task SeedInvestmentAsync()
    {
        using var scope = fixture.Investments.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestmentsDbContext>();

        db.Investments.Add(new Investment
        {
            Id            = InvId,
            UserId        = TestDataSeeder.TestUserId,
            Symbol        = "AAPL",
            Name          = "Apple Inc.",
            Type          = InvestmentType.Stock,
            Quantity      = AaplQty,
            PurchasePrice = AaplPurchase,
            CurrencyCode  = "USD",
            PurchaseDate  = PurchaseDate,
        });

        await db.SaveChangesAsync();
    }
}
