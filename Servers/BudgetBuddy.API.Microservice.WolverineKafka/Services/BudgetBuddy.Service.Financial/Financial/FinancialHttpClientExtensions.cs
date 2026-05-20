using Microsoft.Extensions.Http.Resilience;

namespace BudgetBuddy.Service.Financial.Financial;

/// <summary>
/// Registers Yahoo Finance and CoinGecko HttpClients with resilience handlers,
/// plus the IPriceService orchestrator and price providers.
/// ICurrencyConversionService is registered via AddCurrencyConversionService() — call that first.
/// </summary>
public static class FinancialHttpClientExtensions
{
    internal const string YahooClientName     = "YahooFinancePriceProvider";
    internal const string CoinGeckoClientName = "CoinGeckoPriceProvider";

    public static IServiceCollection AddFinancialHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var priceSettings = configuration.GetSection(PriceServiceSettings.SectionName)
                                .Get<PriceServiceSettings>() ?? new PriceServiceSettings();

        services.Configure<PriceServiceSettings>(
            configuration.GetSection(PriceServiceSettings.SectionName));

        // ── Yahoo Finance ──────────────────────────────────────────────────────────
        services.AddHttpClient(YahooClientName, client =>
            {
                client.BaseAddress = new Uri(priceSettings.YahooFinanceBaseUrl);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 BudgetBuddy/1.0");
            })
            .AddStandardResilienceHandler(opts =>
            {
                opts.TotalRequestTimeout.Timeout      = TimeSpan.FromSeconds(priceSettings.TimeoutSeconds * 3);
                opts.AttemptTimeout.Timeout           = TimeSpan.FromSeconds(priceSettings.TimeoutSeconds);
                opts.Retry.MaxRetryAttempts           = 3;
                opts.Retry.UseJitter                  = true;
                opts.CircuitBreaker.FailureRatio      = 0.5;
                opts.CircuitBreaker.SamplingDuration  = TimeSpan.FromSeconds(30);
                opts.CircuitBreaker.MinimumThroughput = 5;
                opts.CircuitBreaker.BreakDuration     = TimeSpan.FromSeconds(30);
                opts.RateLimiter.DefaultRateLimiterOptions.PermitLimit = 20;
                opts.RateLimiter.DefaultRateLimiterOptions.QueueLimit  = 5;
            });

        // ── CoinGecko ──────────────────────────────────────────────────────────────
        services.AddHttpClient(CoinGeckoClientName, client =>
            {
                client.BaseAddress = new Uri(priceSettings.CoinGeckoBaseUrl);
                if (!string.IsNullOrEmpty(priceSettings.CoinGeckoApiKey))
                {
                    client.DefaultRequestHeaders.Add("x-cg-demo-api-key", priceSettings.CoinGeckoApiKey);
                }
            })
            .AddStandardResilienceHandler(opts =>
            {
                opts.TotalRequestTimeout.Timeout      = TimeSpan.FromSeconds(priceSettings.TimeoutSeconds * 3);
                opts.AttemptTimeout.Timeout           = TimeSpan.FromSeconds(priceSettings.TimeoutSeconds);
                opts.Retry.MaxRetryAttempts           = 2;
                opts.Retry.UseJitter                  = true;
                opts.CircuitBreaker.FailureRatio      = 0.5;
                opts.CircuitBreaker.SamplingDuration  = TimeSpan.FromSeconds(60);
                opts.CircuitBreaker.MinimumThroughput = 3;
                opts.CircuitBreaker.BreakDuration     = TimeSpan.FromSeconds(60);
                opts.RateLimiter.DefaultRateLimiterOptions.PermitLimit = 10;
                opts.RateLimiter.DefaultRateLimiterOptions.QueueLimit  = 3;
            });

        services.AddSingleton<IPriceService, MarketDataPriceService>();
        services.AddSingleton<IPriceProvider, YahooFinancePriceProvider>();
        services.AddSingleton<IPriceProvider, CoinGeckoPriceProvider>();
        services.AddSingleton<IHistoricalPriceProvider, YahooFinancePriceProvider>();
        services.AddSingleton<IHistoricalPriceProvider, CoinGeckoPriceProvider>();

        return services;
    }
}
