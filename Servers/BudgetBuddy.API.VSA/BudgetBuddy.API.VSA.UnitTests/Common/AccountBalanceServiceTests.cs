using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Common;

public class AccountBalanceServiceTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrencyConversionService _currencyConversion = Substitute.For<ICurrencyConversionService>();
    private readonly AccountBalanceService _sut;

    private const string UserId = "user-balance-123";

    public AccountBalanceServiceTests()
    {
        // Alapértelmezett: minden valuta 1:1 arányú (nincs konverzió)
        _currencyConversion
            .ConvertAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1m);

        _sut = new AccountBalanceService(
            _db, _currencyConversion, NullLogger<AccountBalanceService>.Instance);
    }

    // ── CalculateAccountBalancesAsync ────────────────────────────────────────

    [Fact]
    public async Task CalculateBalances_WhenNoAccounts_ReturnsEmptyList()
    {
        var result = await _sut.CalculateAccountBalancesAsync(UserId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateBalances_WithInitialBalanceAndNoTransactions_ReturnsInitialBalance()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            Id = accountId, UserId = UserId,
            Name = "Checking", DefaultCurrencyCode = "USD", InitialBalance = 1000m
        });
        await _db.SaveChangesAsync();

        var result = await _sut.CalculateAccountBalancesAsync(UserId);

        result.Should().HaveCount(1);
        result[0].Balance.Should().Be(1000m);
        result[0].AccountId.Should().Be(accountId);
    }

    [Fact]
    public async Task CalculateBalances_WithIncomeTransactions_AddsToInitialBalance()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            Id = accountId, UserId = UserId,
            Name = "Savings", DefaultCurrencyCode = "USD", InitialBalance = 500m
        });
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), AccountId = accountId, UserId = UserId,
            Amount = 300m, TransactionType = TransactionType.Income,
            CurrencyCode = "USD", TransactionDate = new LocalDate(2026, 1, 15)
        });
        await _db.SaveChangesAsync();

        var result = await _sut.CalculateAccountBalancesAsync(UserId);

        result[0].Balance.Should().Be(800m, "500 initial + 300 income");
    }

    [Fact]
    public async Task CalculateBalances_WithExpenseTransactions_SubtractsFromInitialBalance()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            Id = accountId, UserId = UserId,
            Name = "Main", DefaultCurrencyCode = "USD", InitialBalance = 1000m
        });
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), AccountId = accountId, UserId = UserId,
            Amount = 200m, TransactionType = TransactionType.Expense,
            CurrencyCode = "USD", TransactionDate = new LocalDate(2026, 2, 10)
        });
        await _db.SaveChangesAsync();

        var result = await _sut.CalculateAccountBalancesAsync(UserId);

        result[0].Balance.Should().Be(800m, "1000 initial - 200 expense");
    }

    [Fact]
    public async Task CalculateBalances_WithUpToDate_ExcludesLaterTransactions()
    {
        var accountId = Guid.NewGuid();
        var cutoff = new LocalDate(2026, 3, 1);

        _db.Accounts.Add(new Account
        {
            Id = accountId, UserId = UserId,
            Name = "Test", DefaultCurrencyCode = "USD", InitialBalance = 0m
        });
        _db.Transactions.AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(), AccountId = accountId, UserId = UserId,
                Amount = 100m, TransactionType = TransactionType.Income,
                CurrencyCode = "USD", TransactionDate = new LocalDate(2026, 2, 15) // belül
            },
            new Transaction
            {
                Id = Guid.NewGuid(), AccountId = accountId, UserId = UserId,
                Amount = 500m, TransactionType = TransactionType.Income,
                CurrencyCode = "USD", TransactionDate = new LocalDate(2026, 3, 15) // kívül
            }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.CalculateAccountBalancesAsync(UserId, upToDate: cutoff);

        result[0].Balance.Should().Be(100m, "csak a cutoff előtti tranzakciók számítanak");
    }

    [Fact]
    public async Task CalculateBalances_OnlyReturnsAccountsOfRequestedUser()
    {
        _db.Accounts.AddRange(
            new Account { Id = Guid.NewGuid(), UserId = UserId, Name = "Mine", DefaultCurrencyCode = "USD", InitialBalance = 100m },
            new Account { Id = Guid.NewGuid(), UserId = "other-user", Name = "Theirs", DefaultCurrencyCode = "USD", InitialBalance = 999m }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.CalculateAccountBalancesAsync(UserId);

        result.Should().HaveCount(1);
        result[0].AccountName.Should().Be("Mine");
    }

    [Fact]
    public async Task CalculateBalances_AppliesCurrencyConversionToConvertedBalance()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            Id = accountId, UserId = UserId,
            Name = "HUF Account", DefaultCurrencyCode = "HUF", InitialBalance = 400000m
        });
        await _db.SaveChangesAsync();

        // 1 HUF = 0.0027 USD
        _currencyConversion.ConvertAsync(1m, "HUF", "USD", Arg.Any<CancellationToken>())
            .Returns(0.0027m);

        var result = await _sut.CalculateAccountBalancesAsync(UserId, targetCurrency: "USD");

        result[0].Balance.Should().Be(400000m, "a natív egyenleg valutaváltás nélküli");
        result[0].ConvertedBalance.Should().Be(Math.Round(400000m * 0.0027m, 2));
    }

    [Fact]
    public async Task CalculateBalances_BalancesRoundedTo2DecimalPlaces()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            Id = accountId, UserId = UserId,
            Name = "Rounding Test", DefaultCurrencyCode = "USD", InitialBalance = 0m
        });
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), AccountId = accountId, UserId = UserId,
            Amount = 100m / 3m, // 33.333...
            TransactionType = TransactionType.Income,
            CurrencyCode = "USD", TransactionDate = new LocalDate(2026, 1, 1)
        });
        await _db.SaveChangesAsync();

        var result = await _sut.CalculateAccountBalancesAsync(UserId);

        result[0].Balance.Should().Be(Math.Round(100m / 3m, 2));
    }

    // ── CalculateTotalBalanceAsync ───────────────────────────────────────────

    [Fact]
    public async Task CalculateTotalBalance_SumsConvertedBalancesAcrossAllAccounts()
    {
        _db.Accounts.AddRange(
            new Account { Id = Guid.NewGuid(), UserId = UserId, Name = "A1", DefaultCurrencyCode = "USD", InitialBalance = 1000m },
            new Account { Id = Guid.NewGuid(), UserId = UserId, Name = "A2", DefaultCurrencyCode = "USD", InitialBalance = 500m }
        );
        await _db.SaveChangesAsync();

        var total = await _sut.CalculateTotalBalanceAsync(UserId, targetCurrency: "USD");

        total.Should().Be(1500m);
    }

    public void Dispose() => _db.Dispose();
}
