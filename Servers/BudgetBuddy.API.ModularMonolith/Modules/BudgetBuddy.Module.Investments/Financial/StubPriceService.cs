using BudgetBuddy.Shared.Contracts.Investments;
using BudgetBuddy.Shared.Kernel.Enums;
using Microsoft.Extensions.Logging;

namespace BudgetBuddy.Module.Investments.Financial;

/// <summary>
/// Stub implementation of IPriceService for development/testing.
/// Returns mock prices for investments.
/// </summary>
public class StubPriceService(ILogger<StubPriceService> logger) : IPriceService
{
    private readonly Dictionary<string, decimal> _mockPrices = new()
    {
        { "BTC", 95000m }, { "ETH", 3500m }, { "USDT", 1.0m }, { "BNB", 620m },
        { "SOL", 180m }, { "XRP", 2.5m }, { "ADA", 1.2m }, { "DOGE", 0.35m },
        { "AAPL", 185.50m }, { "GOOGL", 145.30m }, { "MSFT", 420.75m },
        { "AMZN", 178.25m }, { "TSLA", 245.80m }, { "NVDA", 880.50m }, { "META", 512.30m },
        { "VOO", 485.20m }, { "SPY", 495.75m }, { "QQQ", 425.60m },
        { "VTI", 265.40m }, { "IVV", 540.90m }
    };

    public Task<decimal> GetCurrentPriceAsync(string symbol, InvestmentType investmentType,
        string currencyCode = "USD", CancellationToken cancellationToken = default)
    {
        logger.LogDebug("STUB: Getting price for {Symbol} ({Type}) in {Currency}", symbol, investmentType, currencyCode);
        var price = _mockPrices.TryGetValue(symbol.ToUpperInvariant(), out var mockPrice) ? mockPrice : 100m;
        return Task.FromResult(price);
    }

    public Task<Dictionary<string, decimal>> GetBatchPricesAsync(
        List<(string Symbol, InvestmentType Type)> investments,
        string currencyCode = "USD", CancellationToken cancellationToken = default)
    {
        var result = investments.ToDictionary(
            inv => inv.Symbol,
            inv => _mockPrices.TryGetValue(inv.Symbol.ToUpperInvariant(), out var price) ? price : 100m);
        return Task.FromResult(result);
    }
}
