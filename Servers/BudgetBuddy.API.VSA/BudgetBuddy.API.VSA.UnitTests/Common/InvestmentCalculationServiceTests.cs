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
using NSubstitute.ExceptionExtensions;

namespace BudgetBuddy.API.VSA.UnitTests.Common;

public class InvestmentCalculationServiceTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ICurrencyConversionService _currencyConversion = Substitute.For<ICurrencyConversionService>();
    private readonly IPriceService _priceService = Substitute.For<IPriceService>();
    private readonly InvestmentCalculationService _sut;

    private static readonly Instant Now = Instant.FromUtc(2026, 4, 1, 12, 0, 0);

    public InvestmentCalculationServiceTests()
    {
        _clock.GetCurrentInstant().Returns(Now);
        _currencyConversion.ConvertAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1m); // 1:1 alapértelmezett

        _sut = new InvestmentCalculationService(
            _db, _clock, _currencyConversion, _priceService,
            NullLogger<InvestmentCalculationService>.Instance);
    }

    // ── CalculateInvestmentMetrics (pure calculation) ────────────────────────

    [Fact]
    public void CalculateMetrics_CorrectTotalInvested()
    {
        var (totalInvested, _, _, _) = _sut.CalculateInvestmentMetrics(
            quantity: 10, purchasePrice: 50m, currentPrice: 60m);

        totalInvested.Should().Be(500m);
    }

    [Fact]
    public void CalculateMetrics_CorrectCurrentValue()
    {
        var (_, currentValue, _, _) = _sut.CalculateInvestmentMetrics(
            quantity: 10, purchasePrice: 50m, currentPrice: 60m);

        currentValue.Should().Be(600m);
    }

    [Fact]
    public void CalculateMetrics_PositiveReturn_CorrectGainLoss()
    {
        var (_, _, gainLoss, _) = _sut.CalculateInvestmentMetrics(
            quantity: 10, purchasePrice: 50m, currentPrice: 60m);

        gainLoss.Should().Be(100m);
    }

    [Fact]
    public void CalculateMetrics_NegativeReturn_CorrectGainLoss()
    {
        var (_, _, gainLoss, _) = _sut.CalculateInvestmentMetrics(
            quantity: 5, purchasePrice: 100m, currentPrice: 80m);

        gainLoss.Should().Be(-100m);
    }

    [Fact]
    public void CalculateMetrics_GainLossPercentageRoundedTo2Decimals()
    {
        // 1/3 = 33.333...% → round to 33.33%
        var (_, _, _, gainLossPercentage) = _sut.CalculateInvestmentMetrics(
            quantity: 3, purchasePrice: 100m, currentPrice: 133.33m);

        gainLossPercentage.Should().Be(33.33m);
    }

    [Fact]
    public void CalculateMetrics_WhenTotalInvestedIsZero_GainLossPercentageIsZero()
    {
        var (_, _, _, gainLossPercentage) = _sut.CalculateInvestmentMetrics(
            quantity: 0, purchasePrice: 100m, currentPrice: 200m);

        gainLossPercentage.Should().Be(0m, "nullával való osztás helyett 0-t ad vissza");
    }

    // ── GetCurrentPricesAsync – live prices ─────────────────────────────────

    [Fact]
    public async Task GetCurrentPrices_WhenLivePricesAvailable_ReturnsLivePrices()
    {
        var symbols = new List<(string Symbol, InvestmentType Type)>
        {
            ("AAPL", InvestmentType.Stock)
        };
        _priceService.GetBatchPricesAsync(symbols, "USD", Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal> { ["AAPL"] = 150m });

        var result = await _sut.GetCurrentPricesAsync(symbols, "USD");

        result.Should().ContainKey("AAPL").WhoseValue.Should().Be(150m);
    }

    // ── GetCurrentPricesAsync – snapshot fallback ───────────────────────────

    [Fact]
    public async Task GetCurrentPrices_WhenLiveFailsAndRecentSnapshotExists_ReturnsSnapshotPrice()
    {
        _priceService.GetBatchPricesAsync(Arg.Any<List<(string, InvestmentType)>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("API down"));

        // Snapshot within 7 days
        _db.PriceSnapshots.Add(new PriceSnapshot
        {
            Symbol = "BTC",
            Date = Now.InUtc().Date.PlusDays(-2),
            ClosePrice = 50000m
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetCurrentPricesAsync(
            [("BTC", InvestmentType.Crypto)], "USD");

        result.Should().ContainKey("BTC");
        result["BTC"].Should().Be(50000m);
    }

    [Fact]
    public async Task GetCurrentPrices_WhenSnapshotOlderThan7Days_IgnoresIt()
    {
        _priceService.GetBatchPricesAsync(Arg.Any<List<(string, InvestmentType)>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("API down"));

        // Snapshot older than 7 days
        _db.PriceSnapshots.Add(new PriceSnapshot
        {
            Symbol = "ETH",
            Date = Now.InUtc().Date.PlusDays(-10),
            ClosePrice = 3000m
        });
        await _db.SaveChangesAsync();

        var fallback = new Dictionary<string, decimal> { ["ETH"] = 2800m };
        var result = await _sut.GetCurrentPricesAsync(
            [("ETH", InvestmentType.Crypto)], "USD", fallback);

        result.Should().ContainKey("ETH").WhoseValue.Should().Be(2800m,
            "7 napnál régebbi snapshot nem számít, fallback kerül használatra");
    }

    [Fact]
    public async Task GetCurrentPrices_WhenAllFail_ReturnsFallbackPrices()
    {
        _priceService.GetBatchPricesAsync(Arg.Any<List<(string, InvestmentType)>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Network error"));

        var fallback = new Dictionary<string, decimal> { ["MSFT"] = 300m };
        var result = await _sut.GetCurrentPricesAsync(
            [("MSFT", InvestmentType.Stock)], "USD", fallback);

        result.Should().ContainKey("MSFT").WhoseValue.Should().Be(300m);
    }

    [Fact]
    public async Task GetCurrentPrices_WhenAllFailAndNoFallback_ReturnsEmptyDictionary()
    {
        _priceService.GetBatchPricesAsync(Arg.Any<List<(string, InvestmentType)>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Network error"));

        var result = await _sut.GetCurrentPricesAsync(
            [("UNKNOWN", InvestmentType.Stock)], "USD");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCurrentPrices_SnapshotConvertedToTargetCurrency()
    {
        _priceService.GetBatchPricesAsync(Arg.Any<List<(string, InvestmentType)>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("down"));

        // EUR/USD = 1.1 → 1 USD = 1.1 EUR
        _currencyConversion.ConvertAsync(1m, "USD", "EUR", Arg.Any<CancellationToken>())
            .Returns(1.1m);

        _db.PriceSnapshots.Add(new PriceSnapshot
        {
            Symbol = "GOOGL",
            Date = Now.InUtc().Date.PlusDays(-1),
            ClosePrice = 100m // USD
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetCurrentPricesAsync(
            [("GOOGL", InvestmentType.Stock)], "EUR");

        result["GOOGL"].Should().Be(110m, "100 USD * 1.1 = 110 EUR");
    }

    public void Dispose() => _db.Dispose();
}
