namespace BudgetBuddy.Infrastructure.Financial;

public class PriceServiceSettings
{
    public const string SectionName = "PriceService";

    public string CoinGeckoBaseUrl { get; init; } = "https://api.coingecko.com/api/v3";

    /// <summary>Optional CoinGecko Demo API key — increases rate limit from ~30/min to ~50/min.</summary>
    public string? CoinGeckoApiKey { get; init; }

    public string YahooFinanceBaseUrl { get; init; } = "https://query1.finance.yahoo.com";

    /// <summary>How long to cache prices in the hybrid cache (L2). Default: 15 minutes.</summary>
    public int CacheDurationMinutes { get; init; } = 15;

    public int TimeoutSeconds { get; init; } = 10;
}
