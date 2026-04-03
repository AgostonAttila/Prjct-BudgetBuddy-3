
using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;

namespace BudgetBuddy.API.VSA.Common.Shared.Extensions;

/// <summary>
/// Extension methods for ICurrencyConversionService to provide additional functionality
/// </summary>
public static class CurrencyConversionServiceExtensions
{
    /// <summary>
    /// Converts an amount from one currency to another with error handling and fallback.
    /// If conversion fails, returns the original amount.
    /// </summary>
    /// <param name="currencyConversionService">The currency conversion service</param>
    /// <param name="amount">Amount to convert</param>
    /// <param name="sourceCurrency">Source currency code (e.g., "USD", "EUR"). If null, defaults to "USD"</param>
    /// <param name="targetCurrency">Target currency code</param>
    /// <param name="logger">Logger for debugging conversion failures</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Converted amount, or original amount if conversion fails</returns>
    public static async Task<decimal> ConvertWithFallbackAsync(
        this ICurrencyConversionService currencyConversionService,
        decimal amount,
        string? sourceCurrency,
        string targetCurrency,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var fromCurrency = sourceCurrency?.ToUpperInvariant() ?? "USD";
        var toCurrency = targetCurrency.ToUpperInvariant();

        try
        {
            return await currencyConversionService.ConvertAsync(
                amount, fromCurrency, toCurrency, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex,
                "Failed to convert {Amount} from {From} to {To}, using original amount",
                amount, fromCurrency, toCurrency);
            return amount;
        }
    }
}
