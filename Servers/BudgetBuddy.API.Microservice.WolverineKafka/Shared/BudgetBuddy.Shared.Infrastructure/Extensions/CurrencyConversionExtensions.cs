using BudgetBuddy.Shared.Infrastructure.Financial;
using BudgetBuddy.Shared.Messages.Contracts.Financial;
using Microsoft.Extensions.Http.Resilience;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

/// <summary>
/// Registers the Frankfurter FX HTTP client with resilience handlers and
/// the <see cref="ICurrencyConversionService"/> / <see cref="IFxHistoricalProvider"/> singletons.
/// Call this in every service that needs currency conversion.
/// </summary>
public static class CurrencyConversionExtensions
{
    public static IServiceCollection AddCurrencyConversionService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ExchangeRateSettings>(
            configuration.GetSection(ExchangeRateSettings.SectionName));

        var fxSettings = configuration
            .GetSection(ExchangeRateSettings.SectionName)
            .Get<ExchangeRateSettings>() ?? new ExchangeRateSettings();

        // Register a named (non-typed) HttpClient so FrankfurterCurrencyConversionService
        // can call IHttpClientFactory.CreateClient(ClientName) directly.
        // A typed AddHttpClient<T> registration would make T transient, which conflicts
        // with the singleton lifetime we need here.
        services.AddHttpClient(FrankfurterCurrencyConversionService.ClientName, client =>
            {
                client.BaseAddress = new Uri(fxSettings.BaseUrl);
            })
            .AddStandardResilienceHandler(opts =>
            {
                opts.TotalRequestTimeout.Timeout      = TimeSpan.FromSeconds(fxSettings.TimeoutSeconds * 3);
                opts.AttemptTimeout.Timeout           = TimeSpan.FromSeconds(fxSettings.TimeoutSeconds);
                opts.Retry.MaxRetryAttempts           = 3;
                opts.Retry.UseJitter                  = true;
                opts.CircuitBreaker.FailureRatio      = 0.5;
                opts.CircuitBreaker.SamplingDuration  = TimeSpan.FromSeconds(60);
                opts.CircuitBreaker.MinimumThroughput = 3;
                opts.CircuitBreaker.BreakDuration     = TimeSpan.FromSeconds(30);
                opts.RateLimiter.DefaultRateLimiterOptions.PermitLimit = 30;
                opts.RateLimiter.DefaultRateLimiterOptions.QueueLimit  = 10;
            });

        // IHttpClientFactory (singleton) is injected directly, so this can be a true singleton.
        services.AddSingleton<FrankfurterCurrencyConversionService>();
        services.AddSingleton<ICurrencyConversionService>(sp =>
            sp.GetRequiredService<FrankfurterCurrencyConversionService>());
        services.AddSingleton<IFxHistoricalProvider>(sp =>
            (IFxHistoricalProvider)sp.GetRequiredService<ICurrencyConversionService>());

        return services;
    }
}
