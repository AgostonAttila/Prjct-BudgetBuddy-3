namespace BudgetBuddy.API.VSA.Common.Infrastructure.Financial;

/// <summary>
/// Stub implementation of IPriceService for development/testing
/// Returns mock prices for investments
/// TODO: Replace with real implementation (CoinGecko, Alpha Vantage, etc.)
/// </summary>
public class StubPriceService(ILogger<StubPriceService> logger) : IPriceService
{
    // Mock prices for development
    private readonly Dictionary<string, decimal> _mockPrices = new()
    {
        // Crypto
        { "BTC", 95000m },
        { "ETH", 3500m },
        { "USDT", 1.0m },
        { "BNB", 620m },
        { "SOL", 180m },
        { "XRP", 2.5m },
        { "ADA", 1.2m },
        { "DOGE", 0.35m },

        // Stocks
        { "AAPL", 185.50m },
        { "GOOGL", 145.30m },
        { "MSFT", 420.75m },
        { "AMZN", 178.25m },
        { "TSLA", 245.80m },
        { "NVDA", 880.50m },
        { "META", 512.30m },

        // ETFs
        { "VOO", 485.20m },
        { "SPY", 495.75m },
        { "QQQ", 425.60m },
        { "VTI", 265.40m },
        { "IVV", 540.90m }
    };

    public Task<decimal> GetCurrentPriceAsync(
        string symbol,
        InvestmentType investmentType,
        string currencyCode = "USD",
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("STUB: Getting price for {Symbol} ({Type}) in {Currency}",
            symbol, investmentType, currencyCode);

        // Return mock price or default
        var price = _mockPrices.TryGetValue(symbol.ToUpperInvariant(), out var mockPrice)
            ? mockPrice
            : 100m; // Default price for unknown symbols

        logger.LogDebug("STUB: Returning mock price {Price} for {Symbol}", price, symbol);

        return Task.FromResult(price);
    }

    public Task<Dictionary<string, decimal>> GetBatchPricesAsync(
        List<(string Symbol, InvestmentType Type)> investments,
        string currencyCode = "USD",
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("STUB: Getting batch prices for {Count} investments in {Currency}",
            investments.Count, currencyCode);

        var result = investments.ToDictionary(
            inv => inv.Symbol,
            inv => _mockPrices.TryGetValue(inv.Symbol.ToUpperInvariant(), out var price) ? price : 100m
        );

        logger.LogDebug("STUB: Returning {Count} mock prices", result.Count);

        return Task.FromResult(result);
    }
}
