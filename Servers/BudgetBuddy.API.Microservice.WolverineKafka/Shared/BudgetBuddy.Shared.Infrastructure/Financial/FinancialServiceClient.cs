using System.Net.Http.Json;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Contracts.Financial;
using Microsoft.Extensions.Logging;
using NodaTime.Text;

namespace BudgetBuddy.Shared.Infrastructure.Financial;

/// <summary>
/// HTTP client implementation that delegates to BudgetBuddy.Service.Financial.
/// Used by Analytics and Investments services for live and historical market prices.
/// On failure returns an empty dictionary — callers fall back to their local PriceSnapshot data.
/// </summary>
public class FinancialServiceClient(
    HttpClient httpClient,
    ILogger<FinancialServiceClient> logger) : IPriceService, IHistoricalPriceService
{
    public async Task<decimal> GetCurrentPriceAsync(
        string symbol,
        InvestmentType investmentType,
        string currencyCode = "USD",
        CancellationToken cancellationToken = default)
    {
        var prices = await GetBatchPricesAsync([(symbol, investmentType)], currencyCode, cancellationToken);
        return prices.GetValueOrDefault(symbol.ToUpperInvariant(), 0m);
    }

    public async Task<Dictionary<string, decimal>> GetBatchPricesAsync(
        List<(string Symbol, InvestmentType Type)> investments,
        string currencyCode = "USD",
        CancellationToken cancellationToken = default)
    {
        if (investments.Count == 0)
        {
            return [];
        }

        var symbolsParam = string.Join(",", investments.Select(i => $"{i.Symbol}:{i.Type}"));
        var url = $"api/prices/current?symbols={Uri.EscapeDataString(symbolsParam)}&currency={Uri.EscapeDataString(currencyCode)}";

        try
        {
            var result = await httpClient.GetFromJsonAsync<Dictionary<string, decimal>>(url, cancellationToken);
            return result ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Financial service unavailable for symbols: {Symbols}", symbolsParam);
            return [];
        }
    }

    public async Task<Dictionary<LocalDate, decimal>> GetDailyClosePricesAsync(
        string symbol,
        InvestmentType type,
        LocalDate from,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/prices/historical?symbol={Uri.EscapeDataString(symbol)}&type={type}&from={from}";

        try
        {
            var result = await httpClient.GetFromJsonAsync<Dictionary<string, decimal>>(url, cancellationToken);
            if (result is null) { return []; }

            var prices = new Dictionary<LocalDate, decimal>();
            foreach (var (key, value) in result)
            {
                var parseResult = LocalDatePattern.Iso.Parse(key);
                if (parseResult.Success)
                {
                    prices[parseResult.Value] = value;
                }
            }
            return prices;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Financial service unavailable for historical prices: {Symbol}", symbol);
            return [];
        }
    }
}
