using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Features.BudgetAlerts.GetBudgetAlerts;
using BudgetBuddy.API.VSA.Features.BudgetAlerts.Services;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.BudgetAlerts;

public class BudgetAlertServiceTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrencyConversionService _currencyService = Substitute.For<ICurrencyConversionService>();
    private readonly BudgetAlertService _service;

    public BudgetAlertServiceTests()
    {
        _service = new BudgetAlertService(_db, _currencyService, NullLogger<BudgetAlertService>.Instance);
    }

    // ── DetermineAlertLevel ────────────────────────────────────────────────

    [Theory]
    [InlineData(100, AlertLevel.Exceeded)]
    [InlineData(110, AlertLevel.Exceeded)]
    [InlineData(80, AlertLevel.Warning)]
    [InlineData(99.99, AlertLevel.Warning)]
    [InlineData(0, AlertLevel.Safe)]
    [InlineData(79.99, AlertLevel.Safe)]
    public void DetermineAlertLevel_ReturnsCorrectLevel(decimal utilization, AlertLevel expected)
    {
        _service.DetermineAlertLevel(utilization).Should().Be(expected);
    }

    // ── GenerateAlertMessage ───────────────────────────────────────────────

    [Fact]
    public void GenerateAlertMessage_WhenWarning_ContainsPercentage()
    {
        var message = _service.GenerateAlertMessage(AlertLevel.Warning, 85m, 15m);
        message.Should().Contain("85");
        message.Should().Contain("15");
    }

    [Fact]
    public void GenerateAlertMessage_WhenExceeded_ContainsExceededText()
    {
        var message = _service.GenerateAlertMessage(AlertLevel.Exceeded, 120m, -20m);
        message.Should().ContainEquivalentOf("exceeded");
    }

    [Fact]
    public void GenerateAlertMessage_WhenSafe_ReturnsEmpty()
    {
        var message = _service.GenerateAlertMessage(AlertLevel.Safe, 50m, 50m);
        message.Should().BeEmpty();
    }

    // ── CalculateCategorySpending ──────────────────────────────────────────

    [Fact]
    public void CalculateCategorySpending_SumsCorrectly()
    {
        var categoryId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            new() { CategoryId = categoryId, Amount = 100m },
            new() { CategoryId = categoryId, Amount = 50m },
            new() { CategoryId = Guid.NewGuid(), Amount = 999m } // different category
        };

        var result = _service.CalculateCategorySpending(transactions, categoryId);

        result.Should().Be(150m);
    }

    // ── CalculateAlertsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CalculateAlertsAsync_WhenNoBudgets_ReturnsEmpty()
    {
        var result = await _service.CalculateAlertsAsync("user-1", 2024, 6);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateAlertsAsync_WhenSpendingBelowThreshold_ReturnsSafeAlerts()
    {
        const string userId = "user-123";
        var categoryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = userId, Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Categories.Add(new Category { Id = categoryId, UserId = userId, Name = "Food" });
        _db.Budgets.Add(new Budget { Id = Guid.NewGuid(), UserId = userId, CategoryId = categoryId, Name = "Food", Amount = 1000m, CurrencyCode = "USD", Year = 2024, Month = 6 });
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), UserId = userId, AccountId = accountId, CategoryId = categoryId,
            Amount = 100m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
            TransactionDate = new LocalDate(2024, 6, 10)
        });
        await _db.SaveChangesAsync();

        // Only Warning and Exceeded alerts are returned (Safe are filtered out)
        var result = await _service.CalculateAlertsAsync(userId, 2024, 6);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateAlertsAsync_WhenSpendingExceedsBudget_ReturnsExceededAlert()
    {
        const string userId = "user-456";
        var categoryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = userId, Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Categories.Add(new Category { Id = categoryId, UserId = userId, Name = "Entertainment" });
        _db.Budgets.Add(new Budget { Id = Guid.NewGuid(), UserId = userId, CategoryId = categoryId, Name = "Entertainment", Amount = 100m, CurrencyCode = "USD", Year = 2024, Month = 6 });
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), UserId = userId, AccountId = accountId, CategoryId = categoryId,
            Amount = 150m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
            TransactionDate = new LocalDate(2024, 6, 15)
        });
        await _db.SaveChangesAsync();

        var result = await _service.CalculateAlertsAsync(userId, 2024, 6);

        result.Should().HaveCount(1);
        result[0].AlertLevel.Should().Be(AlertLevel.Exceeded);
    }

    public void Dispose() => _db.Dispose();
}
