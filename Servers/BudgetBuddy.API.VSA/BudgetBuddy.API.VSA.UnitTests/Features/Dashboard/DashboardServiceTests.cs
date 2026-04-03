using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Dashboard.Services;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Dashboard;

public class DashboardServiceTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrencyConversionService _currencyService = Substitute.For<ICurrencyConversionService>();
    private readonly IInvestmentCalculationService _investmentCalculationService = Substitute.For<IInvestmentCalculationService>();
    private readonly IAccountBalanceService _accountBalanceService = Substitute.For<IAccountBalanceService>();
    private readonly DashboardService _service;
    private const string UserId = "user-dashboard-1";

    public DashboardServiceTests()
    {
        // Default: same-currency conversions return 1:1 rate
        _currencyService
            .ConvertAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<decimal>(0));

        _service = new DashboardService(
            _db,
            new PassthroughHybridCache(),
            _currencyService,
            _investmentCalculationService,
            _accountBalanceService,
            NullLogger<DashboardService>.Instance);
    }

    // ── GetAccountsSummaryAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAccountsSummaryAsync_WhenNoAccounts_ReturnsZeroSummary()
    {
        var result = await _service.GetAccountsSummaryAsync(UserId, "USD");

        result.AccountCount.Should().Be(0);
        result.TotalBalance.Should().Be(0);
        result.PrimaryCurrency.Should().Be("USD");
    }

    [Fact]
    public async Task GetAccountsSummaryAsync_WithAccounts_ReturnsCombinedBalance()
    {
        _db.Accounts.Add(new Account { Id = Guid.NewGuid(), UserId = UserId, Name = "Checking", DefaultCurrencyCode = "USD" });
        _db.Accounts.Add(new Account { Id = Guid.NewGuid(), UserId = UserId, Name = "Savings", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        _accountBalanceService
            .CalculateTotalBalanceAsync(UserId, "USD", null, Arg.Any<CancellationToken>())
            .Returns(1500m);

        var result = await _service.GetAccountsSummaryAsync(UserId, "USD");

        result.AccountCount.Should().Be(2);
        result.TotalBalance.Should().Be(1500m);
    }

    [Fact]
    public async Task GetAccountsSummaryAsync_WithInvestments_AddedToTotalBalance()
    {
        _db.Accounts.Add(new Account { Id = Guid.NewGuid(), UserId = UserId, Name = "Main", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        _db.Investments.Add(new Investment
        {
            Id = Guid.NewGuid(), UserId = UserId, Symbol = "AAPL", Name = "Apple Inc",
            Type = InvestmentType.Stock, Quantity = 10m, PurchasePrice = 150m,
            CurrencyCode = "USD", PurchaseDate = new LocalDate(2024, 1, 1)
        });
        await _db.SaveChangesAsync();

        _accountBalanceService
            .CalculateTotalBalanceAsync(UserId, "USD", null, Arg.Any<CancellationToken>())
            .Returns(1000m);

        _investmentCalculationService
            .GetCurrentPricesAsync(
                Arg.Any<List<(string, InvestmentType)>>(), "USD",
                Arg.Any<Dictionary<string, decimal>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal> { { "AAPL", 180m } });

        _investmentCalculationService
            .CalculateInvestmentMetrics(10m, 150m, 180m)
            .Returns((1500m, 1800m, 300m, 20m));

        var result = await _service.GetAccountsSummaryAsync(UserId, "USD");

        result.TotalBalance.Should().Be(2800m); // 1000 + 1800
    }

    // ── GetMonthSummaryAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetMonthSummaryAsync_WhenNoTransactions_ReturnsZeroSummary()
    {
        var result = await _service.GetMonthSummaryAsync(UserId, 2024, 6, "USD");

        result.TotalIncome.Should().Be(0);
        result.TotalExpense.Should().Be(0);
        result.NetIncome.Should().Be(0);
        result.TransactionCount.Should().Be(0);
        result.Year.Should().Be(2024);
        result.Month.Should().Be(6);
    }

    [Fact]
    public async Task GetMonthSummaryAsync_WithTransactions_CalculatesNetIncome()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Transactions.AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = 3000m, CurrencyCode = "USD",
                TransactionType = TransactionType.Income,
                TransactionDate = new LocalDate(2024, 6, 10)
            },
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = 800m, CurrencyCode = "USD",
                TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2024, 6, 15)
            }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetMonthSummaryAsync(UserId, 2024, 6, "USD");

        result.TotalIncome.Should().Be(3000m);
        result.TotalExpense.Should().Be(800m);
        result.NetIncome.Should().Be(2200m);
        result.TransactionCount.Should().Be(2);
    }

    // ── GetBudgetSummaryAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetBudgetSummaryAsync_WhenNoBudgets_ReturnsEmptySummary()
    {
        var result = await _service.GetBudgetSummaryAsync(UserId, 2024, 6, "USD");

        result.TotalBudget.Should().Be(0);
        result.TotalSpent.Should().Be(0);
        result.TotalBudgetCount.Should().Be(0);
        result.OverBudgetCount.Should().Be(0);
    }

    [Fact]
    public async Task GetBudgetSummaryAsync_WhenSpendingUnderBudget_ReturnsCorrectUtilization()
    {
        var categoryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Food" });
        _db.Budgets.Add(new Budget
        {
            Id = Guid.NewGuid(), UserId = UserId, CategoryId = categoryId,
            Name = "Food", Amount = 500m, CurrencyCode = "USD", Year = 2024, Month = 6
        });
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId, CategoryId = categoryId,
            Amount = 250m, CurrencyCode = "USD",
            TransactionType = TransactionType.Expense,
            TransactionDate = new LocalDate(2024, 6, 10)
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetBudgetSummaryAsync(UserId, 2024, 6, "USD");

        result.TotalBudget.Should().Be(500m);
        result.TotalSpent.Should().Be(250m);
        result.Remaining.Should().Be(250m);
        result.UtilizationPercentage.Should().Be(50m);
        result.OverBudgetCount.Should().Be(0);
        result.TotalBudgetCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBudgetSummaryAsync_WhenSpendingExceedsBudget_ReportsOverBudget()
    {
        var categoryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Entertainment" });
        _db.Budgets.Add(new Budget
        {
            Id = Guid.NewGuid(), UserId = UserId, CategoryId = categoryId,
            Name = "Entertainment", Amount = 100m, CurrencyCode = "USD", Year = 2024, Month = 6
        });
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId, CategoryId = categoryId,
            Amount = 150m, CurrencyCode = "USD",
            TransactionType = TransactionType.Expense,
            TransactionDate = new LocalDate(2024, 6, 20)
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetBudgetSummaryAsync(UserId, 2024, 6, "USD");

        result.OverBudgetCount.Should().Be(1);
        result.UtilizationPercentage.Should().BeGreaterThan(100m);
    }

    // ── GetTopCategoriesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetTopCategoriesAsync_WhenNoExpenses_ReturnsEmpty()
    {
        var start = new LocalDate(2024, 6, 1);
        var end = new LocalDate(2024, 6, 30);

        var result = await _service.GetTopCategoriesAsync(UserId, start, end, displayCurrency: "USD");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTopCategoriesAsync_ReturnsLimitedToTopCount()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });

        // Add 4 categories with different spending amounts
        for (var i = 1; i <= 4; i++)
        {
            var catId = Guid.NewGuid();
            _db.Categories.Add(new Category { Id = catId, UserId = UserId, Name = $"Cat{i}" });
            _db.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId, CategoryId = catId,
                Amount = i * 100m, CurrencyCode = "USD",
                TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2024, 6, i)
            });
        }

        await _db.SaveChangesAsync();

        var start = new LocalDate(2024, 6, 1);
        var end = new LocalDate(2024, 6, 30);

        var result = await _service.GetTopCategoriesAsync(UserId, start, end, topCount: 2, displayCurrency: "USD");

        result.Should().HaveCount(2);
        // Top 2 by amount (400, 300)
        result[0].Amount.Should().Be(400m);
        result[1].Amount.Should().Be(300m);
    }

    // ── GetRecentTransactionsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetRecentTransactionsAsync_WhenNoTransactions_ReturnsEmpty()
    {
        var result = await _service.GetRecentTransactionsAsync(UserId, count: 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentTransactionsAsync_ReturnsOrderedByDateDescending()
    {
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Main", DefaultCurrencyCode = "USD" });
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Food" });
        _db.Transactions.AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId, CategoryId = categoryId,
                Amount = 50m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2024, 6, 1)
            },
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId, CategoryId = categoryId,
                Amount = 100m, CurrencyCode = "USD", TransactionType = TransactionType.Income,
                TransactionDate = new LocalDate(2024, 6, 15)
            }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetRecentTransactionsAsync(UserId, count: 10);

        result.Should().HaveCount(2);
        result[0].Date.Should().Be("2024-06-15"); // Most recent first
        result[1].Date.Should().Be("2024-06-01");
    }

    [Fact]
    public async Task GetRecentTransactionsAsync_RespectsCountLimit()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Main", DefaultCurrencyCode = "USD" });

        for (var i = 1; i <= 15; i++)
        {
            _db.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = i * 10m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2024, 6, (i % 28) + 1)
            });
        }

        await _db.SaveChangesAsync();

        var result = await _service.GetRecentTransactionsAsync(UserId, count: 5);

        result.Should().HaveCount(5);
    }

    public void Dispose() => _db.Dispose();
}
