namespace BudgetBuddy.Application.Common.Contracts;

/// <summary>
/// Service for investment-related calculations and price fetching
/// </summary>
public interface IInvestmentCalculationService
{
    /// <summary>
    /// Fetches current prices for investments with fallback handling
    /// </summary>
    /// <param name="symbolsWithTypes">List of symbols and their types</param>
    /// <param name="targetCurrency">Currency to fetch prices in</param>
    /// <param name="purchasePriceFallback">Optional fallback prices (symbol -> price)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of symbol -> current price</returns>
    Task<Dictionary<string, decimal>> GetCurrentPricesAsync(
        List<(string Symbol, InvestmentType Type)> symbolsWithTypes,
        string targetCurrency,
        Dictionary<string, decimal>? purchasePriceFallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates investment gain/loss metrics
    /// </summary>
    /// <param name="quantity">Number of shares/units</param>
    /// <param name="purchasePrice">Price per unit at purchase (already converted to target currency)</param>
    /// <param name="currentPrice">Current price per unit (in target currency)</param>
    /// <returns>Tuple of (TotalInvested, CurrentValue, GainLoss, GainLossPercentage)</returns>
    (decimal TotalInvested, decimal CurrentValue, decimal GainLoss, decimal GainLossPercentage)
        CalculateInvestmentMetrics(decimal quantity, decimal purchasePrice, decimal currentPrice);

}
