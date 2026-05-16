using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Service.Transactions.Features.Transactions.Services;
using BudgetBuddy.Service.Transactions.Persistence;
using BudgetBuddy.Shared.Infrastructure.Security.Encryption;
using BudgetBuddy.Shared.Kernel.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NSubstitute;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Transactions;

/// <summary>
/// Integration tests for <see cref="TransactionQueryService"/> against a real PostgreSQL
/// instance (Testcontainers). Verifies the full query path including historical FX
/// grouping logic and decimal precision (#14, #7).
/// </summary>
[Collection(nameof(PostgresCollection))]
public class TransactionQueryServiceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private TransactionsDbContext _db = null!;

    public async Task InitializeAsync()
    {
        _db = new TransactionsDbContext(postgres.CreateOptions<TransactionsDbContext>(), Substitute.For<IEncryptionService>());
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.Transactions.ExecuteDeleteAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task GetCategorySpending_SingleCurrency_ReturnsSummedTotal()
    {
        // Arrange
        var userId     = Guid.NewGuid().ToString();
        var categoryId = Guid.NewGuid();
        var accountId  = Guid.NewGuid();
        var month      = new LocalDate(2025, 3, 1);

        _db.Transactions.AddRange(
            MakeExpense(userId, accountId, categoryId, 100m, "USD", month),
            MakeExpense(userId, accountId, categoryId, 200m, "USD", month.PlusDays(5)));
        await _db.SaveChangesAsync();

        var svc = new TransactionQueryService(_db);

        // Act
        var result = await svc.GetCategorySpendingAsync(
            userId, month, month.PlusMonths(1).PlusDays(-1));

        // Assert
        result.Should().ContainSingle(r => r.CategoryId == categoryId);
        var row = result.Single(r => r.CategoryId == categoryId);
        row.Total.Should().Be(300m);
        row.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public async Task GetCategorySpending_WithRefCurrencyAmount_GroupsAsEur()
    {
        // Arrange — simulate new transactions where RefCurrencyAmount (EUR) was captured
        var userId     = Guid.NewGuid().ToString();
        var categoryId = Guid.NewGuid();
        var accountId  = Guid.NewGuid();
        var month      = new LocalDate(2025, 4, 1);

        var tx1 = MakeExpense(userId, accountId, categoryId, 100m, "USD", month);
        tx1.RefCurrencyAmount = 92m;   // $100 → €92 at transaction time

        var tx2 = MakeExpense(userId, accountId, categoryId, 200m, "USD", month.PlusDays(3));
        tx2.RefCurrencyAmount = 184m;  // $200 → €184 at transaction time

        _db.Transactions.AddRange(tx1, tx2);
        await _db.SaveChangesAsync();

        var svc = new TransactionQueryService(_db);

        // Act
        var result = await svc.GetCategorySpendingAsync(
            userId, month, month.PlusMonths(1).PlusDays(-1));

        // Assert: should be grouped as EUR and summed using RefCurrencyAmount (#7)
        result.Should().ContainSingle(r => r.CategoryId == categoryId);
        var row = result.Single(r => r.CategoryId == categoryId);
        row.CurrencyCode.Should().Be("EUR");
        row.Total.Should().Be(276m);  // 92 + 184
    }

    [Fact]
    public async Task GetCategorySpending_DecimalPrecision_NoRoundingLoss()
    {
        // Arrange
        var userId     = Guid.NewGuid().ToString();
        var categoryId = Guid.NewGuid();
        var accountId  = Guid.NewGuid();
        var month      = new LocalDate(2025, 5, 1);

        _db.Transactions.AddRange(
            MakeExpense(userId, accountId, categoryId, 0.01m, "EUR", month),
            MakeExpense(userId, accountId, categoryId, 0.02m, "EUR", month.PlusDays(1)),
            MakeExpense(userId, accountId, categoryId, 0.03m, "EUR", month.PlusDays(2)));
        await _db.SaveChangesAsync();

        var svc = new TransactionQueryService(_db);

        var result = await svc.GetCategorySpendingAsync(
            userId, month, month.PlusMonths(1).PlusDays(-1));

        result.Single(r => r.CategoryId == categoryId).Total.Should().Be(0.06m);
    }

    [Fact]
    public async Task GetCategorySpending_ExcludesOtherUsers()
    {
        var user1      = Guid.NewGuid().ToString();
        var user2      = Guid.NewGuid().ToString();
        var categoryId = Guid.NewGuid();
        var accountId  = Guid.NewGuid();
        var month      = new LocalDate(2025, 6, 1);

        _db.Transactions.AddRange(
            MakeExpense(user1, accountId, categoryId, 500m, "EUR", month),
            MakeExpense(user2, accountId, categoryId, 999m, "EUR", month));
        await _db.SaveChangesAsync();

        var svc    = new TransactionQueryService(_db);
        var result = await svc.GetCategorySpendingAsync(user1, month, month.PlusMonths(1).PlusDays(-1));

        result.Should().ContainSingle(r => r.CategoryId == categoryId);
        result.Single(r => r.CategoryId == categoryId).Total.Should().Be(500m);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static BudgetBuddy.Service.Transactions.Domain.Transaction MakeExpense(
        string userId, Guid accountId, Guid categoryId,
        decimal amount, string currency, LocalDate date) =>
        new()
        {
            Id              = Guid.NewGuid(),
            UserId          = userId,
            AccountId       = accountId,
            CategoryId      = categoryId,
            Amount          = amount,
            CurrencyCode    = currency,
            TransactionType = TransactionType.Expense,
            PaymentType     = PaymentType.Card,
            TransactionDate = date,
            IsTransfer      = false,
        };
}
