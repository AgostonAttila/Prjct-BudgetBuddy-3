using BudgetBuddy.Shared.Infrastructure.Financial;
using BudgetBuddy.Shared.Messages.Contracts.Financial;
using Microsoft.Extensions.Http.Resilience;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

/// <summary>
/// Registers <see cref="FinancialServiceClient"/> as the <see cref="IPriceService"/> implementation.
/// Requires "FinancialService:BaseUrl" in configuration (e.g. "http://financial-service:8080").
/// </summary>
public static class FinancialServiceClientExtensions
{
    public static IServiceCollection AddFinancialServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["FinancialService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "FinancialService:BaseUrl is not configured. " +
                "Add it to appsettings.json: { \"FinancialService\": { \"BaseUrl\": \"http://financial-service:8080\" } }");

        services.AddHttpClient<FinancialServiceClient>(client =>
            {
    #pragma warning disable S1075 // BaseUrl comes from configuration — not a hardcoded path
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
#pragma warning restore S1075
            })
            .AddStandardResilienceHandler(opts =>
            {
                opts.TotalRequestTimeout.Timeout      = TimeSpan.FromSeconds(15);
                opts.AttemptTimeout.Timeout           = TimeSpan.FromSeconds(5);
                opts.Retry.MaxRetryAttempts           = 2;
                opts.Retry.UseJitter                  = true;
                opts.CircuitBreaker.FailureRatio      = 0.5;
                opts.CircuitBreaker.SamplingDuration  = TimeSpan.FromSeconds(30);
                opts.CircuitBreaker.MinimumThroughput = 3;
                opts.CircuitBreaker.BreakDuration     = TimeSpan.FromSeconds(30);
            });

        services.AddScoped<IPriceService>(sp => sp.GetRequiredService<FinancialServiceClient>());
        services.AddScoped<IHistoricalPriceService>(sp => sp.GetRequiredService<FinancialServiceClient>());

        return services;
    }
}
