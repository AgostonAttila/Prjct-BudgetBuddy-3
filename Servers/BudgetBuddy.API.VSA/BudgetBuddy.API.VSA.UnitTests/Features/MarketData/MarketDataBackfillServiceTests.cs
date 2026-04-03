using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.MarketData.Services;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.MarketData;

public class MarketDataBackfillServiceTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly IHistoricalPriceService _historicalPriceService = Substitute.For<IHistoricalPriceService>();
    private readonly IFxHistoricalProvider _fxHistoricalProvider = Substitute.For<IFxHistoricalProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly MarketDataBackfillService _service;

    public MarketDataBackfillServiceTests()
    {
        _clock.GetCurrentInstant().Returns(Instant.FromUtc(2024, 6, 1, 0, 0));
        _service = new MarketDataBackfillService(
            _db,
            _historicalPriceService,
            _fxHistoricalProvider,
            _clock,
            NullLogger<MarketDataBackfillService>.Instance);
    }

    // ── Price backfill ────────────────────────────────────────────────────────

    [Fact]
    public async Task BackfillAllMissingAsync_WhenNoInvestments_DoesNotCallPriceService()
    {
        _fxHistoricalProvider
            .GetDailyRatesAsync(Arg.Any<LocalDate>(), Arg.Any<LocalDate>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<LocalDate, Dictionary<string, decimal>>());

        await _service.BackfillAllMissingAsync();

        await _historicalPriceService
            .DidNotReceive()
            .GetDailyClosePricesAsync(Arg.Any<string>(), Arg.Any<InvestmentType>(), Arg.Any<LocalDate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackfillAllMissingAsync_WhenPriceSnapshotAlreadyCovers_SkipsSymbol()
    {
        var purchaseDate = new LocalDate(2024, 1, 1);
        _db.Investments.Add(new Investment
        {
            Id = Guid.NewGuid(), UserId = "u1", Symbol = "AAPL",
            Type = InvestmentType.Stock, PurchaseDate = purchaseDate,
            Quantity = 1, PurchasePrice = 100, CurrencyCode = "USD", Name = "Apple"
        });
        _db.PriceSnapshots.Add(new PriceSnapshot
        {
            Symbol = "AAPL", Date = purchaseDate,
            ClosePrice = 150m, Currency = "USD", Source = "yahoo",
            CreatedAt = SystemClock.Instance.GetCurrentInstant()
        });
        await _db.SaveChangesAsync();

        _fxHistoricalProvider
            .GetDailyRatesAsync(Arg.Any<LocalDate>(), Arg.Any<LocalDate>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<LocalDate, Dictionary<string, decimal>>());

        await _service.BackfillAllMissingAsync();

        await _historicalPriceService
            .DidNotReceive()
            .GetDailyClosePricesAsync(Arg.Any<string>(), Arg.Any<InvestmentType>(), Arg.Any<LocalDate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackfillAllMissingAsync_WhenPriceSnapshotMissing_InsertsSnapshots()
    {
        var purchaseDate = new LocalDate(2024, 1, 1);
        _db.Investments.Add(new Investment
        {
            Id = Guid.NewGuid(), UserId = "u1", Symbol = "AAPL",
            Type = InvestmentType.Stock, PurchaseDate = purchaseDate,
            Quantity = 1, PurchasePrice = 100, CurrencyCode = "USD", Name = "Apple"
        });
        await _db.SaveChangesAsync();

        var prices = new Dictionary<LocalDate, decimal>
        {
            { new LocalDate(2024, 1, 1), 150m },
            { new LocalDate(2024, 1, 2), 155m }
        };
        _historicalPriceService
            .GetDailyClosePricesAsync("AAPL", InvestmentType.Stock, purchaseDate, Arg.Any<CancellationToken>())
            .Returns(prices);

        _fxHistoricalProvider
            .GetDailyRatesAsync(Arg.Any<LocalDate>(), Arg.Any<LocalDate>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<LocalDate, Dictionary<string, decimal>>());

        await _service.BackfillAllMissingAsync();

        _db.PriceSnapshots.Count(p => p.Symbol == "AAPL").Should().Be(2);
    }

    // ── FX backfill ───────────────────────────────────────────────────────────

    [Fact]
    public async Task BackfillAllMissingAsync_WhenNoTransactionsOrInvestments_DoesNotCallFxProvider()
    {
        await _service.BackfillAllMissingAsync();

        await _fxHistoricalProvider
            .DidNotReceive()
            .GetDailyRatesAsync(Arg.Any<LocalDate>(), Arg.Any<LocalDate>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackfillAllMissingAsync_WhenFxAlreadyCovered_SkipsFxBackfill()
    {
        var transactionDate = new LocalDate(2024, 3, 1);
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = "u1", Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), UserId = "u1", AccountId = accountId,
            Amount = 50m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
            TransactionDate = transactionDate
        });
        _db.ExchangeRateSnapshots.Add(new ExchangeRateSnapshot
        {
            Currency = "EUR", Date = transactionDate,
            RateToUsd = 1.1m, CreatedAt = SystemClock.Instance.GetCurrentInstant()
        });
        await _db.SaveChangesAsync();

        await _service.BackfillAllMissingAsync();

        await _fxHistoricalProvider
            .DidNotReceive()
            .GetDailyRatesAsync(Arg.Any<LocalDate>(), Arg.Any<LocalDate>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackfillAllMissingAsync_WhenFxMissing_InsertsExchangeRateSnapshots()
    {
        var transactionDate = new LocalDate(2024, 3, 1);
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = "u1", Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), UserId = "u1", AccountId = accountId,
            Amount = 50m, CurrencyCode = "USD", TransactionType = TransactionType.Expense,
            TransactionDate = transactionDate
        });
        await _db.SaveChangesAsync();

        var rates = new Dictionary<LocalDate, Dictionary<string, decimal>>
        {
            {
                transactionDate,
                new Dictionary<string, decimal> { { "EUR", 0.9m }, { "GBP", 0.8m } }
            }
        };
        _fxHistoricalProvider
            .GetDailyRatesAsync(Arg.Any<LocalDate>(), Arg.Any<LocalDate>(), "USD", Arg.Any<CancellationToken>())
            .Returns(rates);

        await _service.BackfillAllMissingAsync();

        _db.ExchangeRateSnapshots.Count().Should().Be(2);
    }

    public void Dispose() => _db.Dispose();
}
