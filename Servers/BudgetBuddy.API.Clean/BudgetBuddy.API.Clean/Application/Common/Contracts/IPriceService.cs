namespace BudgetBuddy.Application.Common.Contracts;

public interface IPriceService
{
    /// <summary>
    /// Get current price for an investment symbol
    /// </summary>
    /// <param name="symbol">Investment symbol (e.g., BTC, AAPL, VOO)</param>
    /// <param name="investmentType">Type of investment (Crypto, ETF, Stock)</param>
    /// <param name="currencyCode">Target currency code (e.g., USD, EUR)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current price in the specified currency</returns>
    Task<decimal> GetCurrentPriceAsync(
        string symbol,
        InvestmentType investmentType,
        string currencyCode = "USD",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current prices for multiple symbols in batch
    /// </summary>
    Task<Dictionary<string, decimal>> GetBatchPricesAsync(
        List<(string Symbol, InvestmentType Type)> investments,
        string currencyCode = "USD",
        CancellationToken cancellationToken = default);
}
