using BudgetBuddy.Shared.Contracts;
using BudgetBuddy.Shared.Contracts.Financial;
using BudgetBuddy.Shared.Contracts.Investments;
using BudgetBuddy.Module.Investments.Financial;
using BudgetBuddy.Module.Investments.Financial.Providers;
using BudgetBuddy.Module.Investments.Services;
using BudgetBuddy.Module.Investments.Features.Services;
using BudgetBuddy.Module.Investments.Features.MarketData.Services;
using BudgetBuddy.Module.Investments.Features.Investments.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using System.Net;

namespace BudgetBuddy.Module.Investments;

public class InvestmentsModule : IModule
{
    public string ModuleName => "Investments";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<InvestmentsDbContext>(configuration);

        services.AddOptions<ExchangeRateSettings>()
            .Bind(configuration.GetSection(ExchangeRateSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PriceServiceSettings>()
            .Bind(configuration.GetSection(PriceServiceSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IInvestmentCalculationService, InvestmentCalculationService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IPriceService, MarketDataPriceService>();
        services.AddScoped<IHistoricalPriceService, MarketDataHistoricalPriceService>();
        services.AddScoped<IMarketDataBackfillService, MarketDataBackfillService>();
        services.AddScoped<IInvestmentDataService, InvestmentDataService>();

        AddCurrencyConversion(services);
        AddPriceProviders(services);

        return services;
    }

    private static void AddCurrencyConversion(IServiceCollection services)
    {
        services.AddHttpClient<FrankfurterCurrencyConversionService>((sp, client) =>
            {
                var s = sp.GetRequiredService<IOptions<ExchangeRateSettings>>().Value;
                client.BaseAddress = new Uri(s.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(s.TimeoutSeconds);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromSeconds(2);
                options.Retry.BackoffType = DelayBackoffType.Exponential;
            });

        services.AddScoped<ICurrencyConversionService>(
            sp => sp.GetRequiredService<FrankfurterCurrencyConversionService>());

        services.AddScoped<IFxHistoricalProvider>(
            sp => sp.GetRequiredService<FrankfurterCurrencyConversionService>());
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }

    private static void AddPriceProviders(IServiceCollection services)
    {
        services.AddHttpClient<CoinGeckoPriceProvider>((sp, client) =>
            {
                var s = sp.GetRequiredService<IOptions<PriceServiceSettings>>().Value;
                client.BaseAddress = new Uri(s.CoinGeckoBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(s.TimeoutSeconds);
                if (!string.IsNullOrEmpty(s.CoinGeckoApiKey))
                    client.DefaultRequestHeaders.Add("x-cg-demo-api-key", s.CoinGeckoApiKey);
            })
            .AddResilienceHandler("coingecko", builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = args => args.Outcome switch
                    {
                        { Exception: HttpRequestException } => PredicateResult.True(),
                        { Result.StatusCode: HttpStatusCode.TooManyRequests } => PredicateResult.False(),
                        { Result: { IsSuccessStatusCode: false } } => PredicateResult.True(),
                        _ => PredicateResult.False()
                    }
                });
                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    FailureRatio = 0.8,
                    MinimumThroughput = 3,
                    BreakDuration = TimeSpan.FromSeconds(15)
                });
            });

        services.AddHttpClient<YahooFinancePriceProvider>((sp, client) =>
            {
                var s = sp.GetRequiredService<IOptions<PriceServiceSettings>>().Value;
                client.BaseAddress = new Uri(s.YahooFinanceBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(s.TimeoutSeconds);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; BudgetBuddy/1.0)");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                CookieContainer = new System.Net.CookieContainer(),
                UseCookies = true,
                AllowAutoRedirect = true,
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(20);
            });

        services.AddScoped<IPriceProvider>(sp => sp.GetRequiredService<CoinGeckoPriceProvider>());
        services.AddScoped<IPriceProvider>(sp => sp.GetRequiredService<YahooFinancePriceProvider>());
        services.AddScoped<IHistoricalPriceProvider>(sp => sp.GetRequiredService<CoinGeckoPriceProvider>());
        services.AddScoped<IHistoricalPriceProvider>(sp => sp.GetRequiredService<YahooFinancePriceProvider>());
    }
}
