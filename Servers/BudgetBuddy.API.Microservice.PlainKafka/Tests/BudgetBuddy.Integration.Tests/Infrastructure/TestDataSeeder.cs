using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Service.Analytics.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using BudgetBuddy.Service.Investments.Domain;
using BudgetBuddy.Service.Investments.Persistence;
using BudgetBuddy.Service.Transactions.Persistence;
using BudgetBuddy.Shared.Kernel.Enums;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using AnalyticsAccountSnapshot  = BudgetBuddy.Service.Analytics.ReadModels.AccountSnapshot;
using AnalyticsCategorySnapshot  = BudgetBuddy.Service.Analytics.ReadModels.CategorySnapshot;
using InvestmentsAccountSnapshot = BudgetBuddy.Service.Investments.ReadModels.AccountSnapshot;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

/// <summary>
/// Seeds deterministic test data into each service's database.
/// Uses stable GUIDs so expected values can be calculated ahead of time.
///
/// Stub exchange rates (StubCurrencyConversionService, USD base):
///   USD = 1.00, HUF = 370.50
/// Conversion: amount / rates[from] * rates[to]
///
/// Expected portfolio (USD): AccountBalance=2000, InvestmentValue=3350, Total=5350
/// Expected portfolio (HUF): AccountBalance=741000, InvestmentValue=1241175, Total=1982175
/// </summary>
public static class TestDataSeeder
{
    // ── Stable IDs ─────────────────────────────────────────────────────────────
    public static readonly string TestUserId       = TestAuthHandler.TestUserId;
    public static readonly Guid   Account1Id       = Guid.Parse("10000000-0000-0000-0000-000000000010");
    public static readonly Guid   Account2Id       = Guid.Parse("10000000-0000-0000-0000-000000000020");
    public static readonly Guid   Investment1Id    = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid   Investment2Id    = Guid.Parse("20000000-0000-0000-0000-000000000002");
    public static readonly Guid   SalaryCategory  = Guid.Parse("30000000-0000-0000-0000-000000000001");
    public static readonly Guid   RentCategory    = Guid.Parse("30000000-0000-0000-0000-000000000002");
    public static readonly Guid   FoodCategory    = Guid.Parse("30000000-0000-0000-0000-000000000003");

    // ── Account balances ───────────────────────────────────────────────────────
    public const decimal Account1Balance = 1000.00m;   // USD
    public const string  Account1Currency = "USD";
    public const decimal Account2Balance = 370500.00m; // HUF → 1000 USD at stub rate
    public const string  Account2Currency = "HUF";

    // ── Investment prices ──────────────────────────────────────────────────────
    public const decimal AaplPurchasePrice = 150m;
    public const decimal AaplCurrentPrice  = 175m;
    public const decimal AaplQuantity      = 10m;
    public const decimal MsftPurchasePrice = 300m;
    public const decimal MsftCurrentPrice  = 320m;
    public const decimal MsftQuantity      = 5m;

    // ── Transaction amounts ───────────────────────────────────────────────────
    public const decimal IncomeAmount  = 3000m;
    public const decimal RentAmount    = 1000m;
    public const decimal FoodMarch     = 300m;
    public const decimal FoodApril     = 200m;

    // ── Precomputed expected values ────────────────────────────────────────────
    // StubCurrencyConversionService formula: amount / rates[from] * rates[to]
    public const decimal HufRate = 370.50m;

    // Portfolio
    public const decimal ExpectedAccountBalanceUsd      = Account1Balance + Account2Balance / HufRate; // 2000
    public const decimal ExpectedInvestmentValueUsd     = AaplQuantity * AaplCurrentPrice + MsftQuantity * MsftCurrentPrice; // 3350
    public const decimal ExpectedTotalPortfolioUsd      = ExpectedAccountBalanceUsd + ExpectedInvestmentValueUsd; // 5350

    public static readonly decimal ExpectedAccountBalanceHuf  = Account1Balance * HufRate + Account2Balance; // 741000
    public static readonly decimal ExpectedInvestmentValueHuf = ExpectedInvestmentValueUsd * HufRate; // 1241175
    public static readonly decimal ExpectedTotalPortfolioHuf  = ExpectedAccountBalanceHuf + ExpectedInvestmentValueHuf; // 1982175

    // Dashboard: AccountBalance uses transaction-adjusted snapshot (Analytics AccountBalanceService),
    // plus InvestmentValue. The Investments service uses raw snapshots (no tx adjustment).
    // Account1 adjusted: 1000 + 3000 - 1500 = 2500 USD; Account2: 1000 USD; total accounts: 3500
    public const decimal ExpectedAdjustedAccount1BalanceUsd =
        Account1Balance + IncomeAmount - RentAmount - FoodMarch - FoodApril; // 2500
    public const decimal ExpectedAdjustedTotalAccountBalanceUsd =
        ExpectedAdjustedAccount1BalanceUsd + Account2Balance / HufRate;       // 3500
    public const decimal ExpectedDashboardTotalBalanceUsd =
        ExpectedAdjustedTotalAccountBalanceUsd + ExpectedInvestmentValueUsd;  // 6850
    public static readonly decimal ExpectedDashboardTotalBalanceHuf =
        ExpectedDashboardTotalBalanceUsd * HufRate;                           // 2537925

    // Reports
    public const decimal ExpectedTotalIncome  = IncomeAmount;                     // 3000
    public const decimal ExpectedTotalExpense = RentAmount + FoodMarch + FoodApril; // 1500
    public const decimal ExpectedNetIncome    = ExpectedTotalIncome - ExpectedTotalExpense; // 1500

    public static readonly decimal ExpectedTotalIncomeHuf  = ExpectedTotalIncome  * HufRate;
    public static readonly decimal ExpectedTotalExpenseHuf = ExpectedTotalExpense * HufRate;

    public const decimal ExpectedAprilExpense = FoodApril; // 200
    public static readonly decimal ExpectedAprilExpenseHuf = FoodApril * HufRate;

    public const decimal ExpectedFoodSpending = FoodMarch + FoodApril; // 500
    public static readonly decimal ExpectedFoodSpendingHuf = ExpectedFoodSpending * HufRate;

    // ── Seeding methods ────────────────────────────────────────────────────────

    public static async Task SeedInvestmentsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestmentsDbContext>();
        await db.Database.MigrateAsync();

        var today = LocalDate.FromDateTime(DateTime.UtcNow);

        db.AccountSnapshots.AddRange(
            new InvestmentsAccountSnapshot
            {
                AccountId = Account1Id,
                UserId    = TestUserId,
                Name      = "USD Savings",
                Currency  = Account1Currency,
                Balance   = Account1Balance,
                IsActive  = true,
                Version   = 1,
                SyncedAt  = DateTime.UtcNow,
            },
            new InvestmentsAccountSnapshot
            {
                AccountId = Account2Id,
                UserId    = TestUserId,
                Name      = "HUF Savings",
                Currency  = Account2Currency,
                Balance   = Account2Balance,
                IsActive  = true,
                Version   = 1,
                SyncedAt  = DateTime.UtcNow,
            }
        );

        db.Investments.AddRange(
            new Investment
            {
                Id            = Investment1Id,
                UserId        = TestUserId,
                Symbol        = "AAPL",
                Name          = "Apple Inc.",
                Type          = InvestmentType.Stock,
                Quantity      = AaplQuantity,
                PurchasePrice = AaplPurchasePrice,
                CurrencyCode  = "USD",
                PurchaseDate  = new LocalDate(2026, 1, 10),
            },
            new Investment
            {
                Id            = Investment2Id,
                UserId        = TestUserId,
                Symbol        = "MSFT",
                Name          = "Microsoft Corp.",
                Type          = InvestmentType.Stock,
                Quantity      = MsftQuantity,
                PurchasePrice = MsftPurchasePrice,
                CurrencyCode  = "USD",
                PurchaseDate  = new LocalDate(2026, 1, 15),
            }
        );

        // PriceSnapshots: seeded for today so the 7-day lookback window always includes them
        db.PriceSnapshots.AddRange(
            new PriceSnapshot
            {
                Symbol     = "AAPL",
                Date       = today,
                ClosePrice = AaplCurrentPrice,
                Currency   = "USD",
                Source     = "test",
                CreatedAt  = SystemClock.Instance.GetCurrentInstant(),
            },
            new PriceSnapshot
            {
                Symbol     = "MSFT",
                Date       = today,
                ClosePrice = MsftCurrentPrice,
                Currency   = "USD",
                Source     = "test",
                CreatedAt  = SystemClock.Instance.GetCurrentInstant(),
            }
        );

        await db.SaveChangesAsync();
    }

    public static async Task SeedTransactionsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        await db.Database.MigrateAsync();

        db.CategorySnapshots.AddRange(
            new Service.Transactions.ReadModels.CategorySnapshot { CategoryId = SalaryCategory, UserId = TestUserId, Name = "Salary",  SyncedAt = DateTime.UtcNow },
            new Service.Transactions.ReadModels.CategorySnapshot { CategoryId = RentCategory,   UserId = TestUserId, Name = "Rent",    SyncedAt = DateTime.UtcNow },
            new Service.Transactions.ReadModels.CategorySnapshot { CategoryId = FoodCategory,   UserId = TestUserId, Name = "Food",    SyncedAt = DateTime.UtcNow }
        );

        db.Transactions.AddRange(
            MakeTx(TransactionType.Income,  IncomeAmount, "USD", new LocalDate(2026, 1, 15), SalaryCategory),
            MakeTx(TransactionType.Expense, RentAmount,   "USD", new LocalDate(2026, 2,  1), RentCategory),
            MakeTx(TransactionType.Expense, FoodMarch,    "USD", new LocalDate(2026, 3, 15), FoodCategory),
            MakeTx(TransactionType.Expense, FoodApril,    "USD", new LocalDate(2026, 4, 20), FoodCategory)
        );

        await db.SaveChangesAsync();
    }

    public static async Task SeedAnalyticsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        await db.Database.MigrateAsync();

        var today = LocalDate.FromDateTime(DateTime.UtcNow);

        db.AccountSnapshots.AddRange(
            new AnalyticsAccountSnapshot
            {
                AccountId = Account1Id, UserId = TestUserId,
                Name = "USD Savings", Currency = Account1Currency,
                Balance = Account1Balance, IsActive = true,
                Version = 1, SyncedAt = DateTime.UtcNow,
            },
            new AnalyticsAccountSnapshot
            {
                AccountId = Account2Id, UserId = TestUserId,
                Name = "HUF Savings", Currency = Account2Currency,
                Balance = Account2Balance, IsActive = true,
                Version = 1, SyncedAt = DateTime.UtcNow,
            }
        );

        db.CategorySnapshots.AddRange(
            new AnalyticsCategorySnapshot { CategoryId = SalaryCategory, UserId = TestUserId, Name = "Salary", SyncedAt = DateTime.UtcNow },
            new AnalyticsCategorySnapshot { CategoryId = RentCategory,   UserId = TestUserId, Name = "Rent",   SyncedAt = DateTime.UtcNow },
            new AnalyticsCategorySnapshot { CategoryId = FoodCategory,   UserId = TestUserId, Name = "Food",   SyncedAt = DateTime.UtcNow }
        );

        db.Transactions.AddRange(
            MakeAnalyticsTx(TransactionType.Income,  IncomeAmount, "USD", new LocalDate(2026, 1, 15), SalaryCategory),
            MakeAnalyticsTx(TransactionType.Expense, RentAmount,   "USD", new LocalDate(2026, 2,  1), RentCategory),
            MakeAnalyticsTx(TransactionType.Expense, FoodMarch,    "USD", new LocalDate(2026, 3, 15), FoodCategory),
            MakeAnalyticsTx(TransactionType.Expense, FoodApril,    "USD", new LocalDate(2026, 4, 20), FoodCategory)
        );

        db.Investments.AddRange(
            new InvestmentReadModel
            {
                InvestmentId  = Investment1Id, UserId = TestUserId,
                Symbol = "AAPL", Name = "Apple Inc.",
                Type = InvestmentType.Stock, Quantity = AaplQuantity,
                PurchasePrice = AaplPurchasePrice, CurrencyCode = "USD",
                PurchaseDate = new LocalDate(2026, 1, 10),
                SyncedAt = DateTime.UtcNow,
            },
            new InvestmentReadModel
            {
                InvestmentId  = Investment2Id, UserId = TestUserId,
                Symbol = "MSFT", Name = "Microsoft Corp.",
                Type = InvestmentType.Stock, Quantity = MsftQuantity,
                PurchasePrice = MsftPurchasePrice, CurrencyCode = "USD",
                PurchaseDate = new LocalDate(2026, 1, 15),
                SyncedAt = DateTime.UtcNow,
            }
        );

        db.PriceSnapshots.AddRange(
            new PriceSnapshotReadModel
            {
                Symbol = "AAPL", Date = today,
                PriceUsd = AaplCurrentPrice,
                SyncedAt = SystemClock.Instance.GetCurrentInstant(),
            },
            new PriceSnapshotReadModel
            {
                Symbol = "MSFT", Date = today,
                PriceUsd = MsftCurrentPrice,
                SyncedAt = SystemClock.Instance.GetCurrentInstant(),
            }
        );

        await db.SaveChangesAsync();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Service.Transactions.Domain.Transaction MakeTx(
        TransactionType type, decimal amount, string currency, LocalDate date, Guid categoryId) =>
        new()
        {
            Id              = Guid.NewGuid(),
            UserId          = TestUserId,
            AccountId       = Account1Id,
            CategoryId      = categoryId,
            Amount          = amount,
            CurrencyCode    = currency,
            TransactionType = type,
            PaymentType     = PaymentType.Card,
            TransactionDate = date,
            IsTransfer      = false,
            IsHidden        = false,
        };

    private static TransactionReadModel MakeAnalyticsTx(
        TransactionType type, decimal amount, string currency, LocalDate date, Guid categoryId) =>
        new()
        {
            TransactionId   = Guid.NewGuid(),
            UserId          = TestUserId,
            AccountId       = Account1Id,
            CategoryId      = categoryId,
            Amount          = amount,
            CurrencyCode    = currency,
            TransactionType = type,
            TransactionDate = date,
            SyncedAt        = DateTime.UtcNow,
        };
}
