using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Reports.Services;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Reports;

public class ReportServiceTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrencyConversionService _currencyService = Substitute.For<ICurrencyConversionService>();
    private readonly IInvestmentCalculationService _investmentCalculationService = Substitute.For<IInvestmentCalculationService>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ReportService _service;
    private const string UserId = "user-reports-1";

    public ReportServiceTests()
    {
        // Default: same-currency conversions return 1:1 rate
        _currencyService
            .ConvertAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<decimal>(0));

        // Default clock: fixed instant in 2024
        _clock.GetCurrentInstant().Returns(Instant.FromUtc(2024, 6, 30, 0, 0));

        _service = new ReportService(
            _db,
            _clock,
            _investmentCalculationService,
            _currencyService,
            NullLogger<ReportService>.Instance);
    }

    // ── CalculateIncomeVsExpenseAsync ─────────────────────────────────────────

    [Fact]
    public async Task CalculateIncomeVsExpenseAsync_WhenNoTransactions_ReturnsAllZeros()
    {
        var (income, expense, net, incCount, expCount) =
            await _service.CalculateIncomeVsExpenseAsync(UserId, displayCurrency: "USD");

        income.Should().Be(0);
        expense.Should().Be(0);
        net.Should().Be(0);
        incCount.Should().Be(0);
        expCount.Should().Be(0);
    }

    [Fact]
    public async Task CalculateIncomeVsExpenseAsync_WithMixedTransactions_CalculatesCorrectTotals()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Checking", DefaultCurrencyCode = "USD" });
        _db.Transactions.AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = 5000m, CurrencyCode = "USD", TransactionType = TransactionType.Income,
                TransactionDate = new LocalDate(2024, 1, 5)
            },
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = 1200m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2024, 1, 10)
            },
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = 800m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2024, 1, 20)
            }
        );
        await _db.SaveChangesAsync();

        var (income, expense, net, incCount, expCount) =
            await _service.CalculateIncomeVsExpenseAsync(UserId, displayCurrency: "USD");

        income.Should().Be(5000m);
        expense.Should().Be(2000m);
        net.Should().Be(3000m);
        incCount.Should().Be(1);
        expCount.Should().Be(2);
    }

    [Fact]
    public async Task CalculateIncomeVsExpenseAsync_WithDateFilter_OnlyIncludesTransactionsInRange()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Transactions.AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = 1000m, CurrencyCode = "USD", TransactionType = TransactionType.Income,
                TransactionDate = new LocalDate(2024, 3, 10) // within range
            },
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = 9999m, CurrencyCode = "USD", TransactionType = TransactionType.Income,
                TransactionDate = new LocalDate(2024, 1, 5) // outside range
            }
        );
        await _db.SaveChangesAsync();

        var (income, _, _, _, _) = await _service.CalculateIncomeVsExpenseAsync(
            UserId,
            startDate: new LocalDate(2024, 3, 1),
            endDate: new LocalDate(2024, 3, 31),
            displayCurrency: "USD");

        income.Should().Be(1000m);
    }

    // ── GetMonthlyBreakdownAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetMonthlyBreakdownAsync_WhenNoTransactions_ReturnsEmptyList()
    {
        var result = await _service.GetMonthlyBreakdownAsync(UserId, displayCurrency: "USD");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMonthlyBreakdownAsync_GroupsTransactionsByMonth()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Transactions.AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = 2000m, CurrencyCode = "USD", TransactionType = TransactionType.Income,
                TransactionDate = new LocalDate(2024, 4, 5)
            },
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = 500m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2024, 4, 10)
            },
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = 3000m, CurrencyCode = "USD", TransactionType = TransactionType.Income,
                TransactionDate = new LocalDate(2024, 5, 5)
            }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetMonthlyBreakdownAsync(UserId, displayCurrency: "USD");

        result.Should().HaveCount(2);

        var april = result.First(m => m.Month == 4);
        april.Income.Should().Be(2000m);
        april.Expense.Should().Be(500m);
        april.Net.Should().Be(1500m);

        var may = result.First(m => m.Month == 5);
        may.Income.Should().Be(3000m);
        may.Expense.Should().Be(0m);
        may.Net.Should().Be(3000m);
    }

    // ── CalculateSpendingByCategoryAsync ──────────────────────────────────────

    [Fact]
    public async Task CalculateSpendingByCategoryAsync_WhenNoExpenses_ReturnsZeroAndEmptyList()
    {
        var (total, categories) = await _service.CalculateSpendingByCategoryAsync(UserId, displayCurrency: "USD");

        total.Should().Be(0);
        categories.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateSpendingByCategoryAsync_AggregatesSpendingPerCategory()
    {
        var accountId = Guid.NewGuid();
        var foodCatId = Guid.NewGuid();
        var rentCatId = Guid.NewGuid();

        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Main", DefaultCurrencyCode = "USD" });
        _db.Categories.AddRange(
            new Category { Id = foodCatId, UserId = UserId, Name = "Food" },
            new Category { Id = rentCatId, UserId = UserId, Name = "Rent" }
        );
        _db.Transactions.AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId, CategoryId = foodCatId,
                Amount = 200m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2024, 6, 5)
            },
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId, CategoryId = foodCatId,
                Amount = 150m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2024, 6, 12)
            },
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId, CategoryId = rentCatId,
                Amount = 1000m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2024, 6, 1)
            }
        );
        await _db.SaveChangesAsync();

        var (total, categories) = await _service.CalculateSpendingByCategoryAsync(UserId, displayCurrency: "USD");

        total.Should().Be(1350m);
        categories.Should().HaveCount(2);

        var rent = categories.First(c => c.CategoryName == "Rent");
        rent.Amount.Should().Be(1000m);
        rent.Percentage.Should().BeApproximately(74.07m, 0.1m);

        var food = categories.First(c => c.CategoryName == "Food");
        food.Amount.Should().Be(350m);
        food.TransactionCount.Should().Be(2);
    }

    [Fact]
    public async Task CalculateSpendingByCategoryAsync_ExcludesIncomeTransactions()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Main", DefaultCurrencyCode = "USD" });
        _db.Transactions.AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = 5000m, CurrencyCode = "USD", TransactionType = TransactionType.Income,
                TransactionDate = new LocalDate(2024, 6, 1)
            },
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId,
                Amount = 100m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2024, 6, 5)
            }
        );
        await _db.SaveChangesAsync();

        var (total, _) = await _service.CalculateSpendingByCategoryAsync(UserId, displayCurrency: "USD");

        total.Should().Be(100m);
    }

    // ── CalculateInvestmentPerformanceAsync ───────────────────────────────────

    [Fact]
    public async Task CalculateInvestmentPerformanceAsync_WhenNoInvestments_ReturnsAllZeros()
    {
        var (invested, value, gainLoss, gainLossPct, investments) =
            await _service.CalculateInvestmentPerformanceAsync(UserId, displayCurrency: "USD");

        invested.Should().Be(0);
        value.Should().Be(0);
        gainLoss.Should().Be(0);
        gainLossPct.Should().Be(0);
        investments.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateInvestmentPerformanceAsync_WithInvestments_CalculatesGainLoss()
    {
        _db.Investments.Add(new Investment
        {
            Id = Guid.NewGuid(), UserId = UserId,
            Symbol = "AAPL", Name = "Apple Inc",
            Type = InvestmentType.Stock,
            Quantity = 10m,
            PurchasePrice = 150m,
            CurrencyCode = "USD",
            PurchaseDate = new LocalDate(2023, 1, 1)
        });
        await _db.SaveChangesAsync();

        // Mock current price fetch
        _investmentCalculationService
            .GetCurrentPricesAsync(
                Arg.Any<List<(string Symbol, InvestmentType Type)>>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, decimal>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal> { ["AAPL"] = 180m });

        // Mock metrics calculation
        _investmentCalculationService
            .CalculateInvestmentMetrics(10m, 150m, 180m)
            .Returns((TotalInvested: 1500m, CurrentValue: 1800m, GainLoss: 300m, GainLossPercentage: 20m));

        var (invested, value, gainLoss, gainLossPct, investmentList) =
            await _service.CalculateInvestmentPerformanceAsync(UserId, displayCurrency: "USD");

        invested.Should().Be(1500m);
        value.Should().Be(1800m);
        gainLoss.Should().Be(300m);
        gainLossPct.Should().Be(20m);
        investmentList.Should().HaveCount(1);
        investmentList[0].Symbol.Should().Be("AAPL");
    }

    // ── CalculateMonthlySummaryAsync ──────────────────────────────────────────

    [Fact]
    public async Task CalculateMonthlySummaryAsync_WhenNoTransactions_ReturnsZeroTotals()
    {
        var (income, expense, net, startBal, endBal, txCount, topCats, dailyBreakdown) =
            await _service.CalculateMonthlySummaryAsync(UserId, 2024, 6, displayCurrency: "USD");

        income.Should().Be(0);
        expense.Should().Be(0);
        net.Should().Be(0);
        txCount.Should().Be(0);
        topCats.Should().BeEmpty();
        dailyBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateMonthlySummaryAsync_WithTransactions_PopulatesDailyBreakdown()
    {
        var accountId = Guid.NewGuid();
        var catId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Main", DefaultCurrencyCode = "USD", InitialBalance = 0 });
        _db.Categories.Add(new Category { Id = catId, UserId = UserId, Name = "Groceries" });
        _db.Transactions.AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId, CategoryId = catId,
                Amount = 4000m, CurrencyCode = "USD", TransactionType = TransactionType.Income,
                TransactionDate = new LocalDate(2024, 6, 1)
            },
            new Transaction
            {
                Id = Guid.NewGuid(), UserId = UserId, AccountId = accountId, CategoryId = catId,
                Amount = 300m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2024, 6, 5)
            }
        );
        await _db.SaveChangesAsync();

        var (income, expense, net, _, _, txCount, topCats, dailyBreakdown) =
            await _service.CalculateMonthlySummaryAsync(UserId, 2024, 6, displayCurrency: "USD");

        income.Should().Be(4000m);
        expense.Should().Be(300m);
        net.Should().Be(3700m);
        txCount.Should().Be(2);
        topCats.Should().HaveCount(1);
        topCats[0].CategoryName.Should().Be("Groceries");
        dailyBreakdown.Should().NotBeEmpty();

        var day5 = dailyBreakdown.First(d => d.Day == 5);
        day5.Expense.Should().Be(300m);
    }

    public void Dispose() => _db.Dispose();
}
