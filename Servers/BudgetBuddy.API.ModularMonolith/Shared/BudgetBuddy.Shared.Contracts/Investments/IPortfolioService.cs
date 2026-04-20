namespace BudgetBuddy.Shared.Contracts.Investments;

/// <summary>
/// Service for calculating portfolio values and investment performance
/// </summary>
public interface IPortfolioService
{
    /// <summary>
    /// Calculates total portfolio value (accounts + investments) for a user with detailed breakdown
    /// </summary>
    Task<(decimal AccountBalance, decimal InvestmentValue, List<PortfolioAccountBalanceDto> AccountBreakdown, List<PortfolioInvestmentValueDto> InvestmentBreakdown)> CalculatePortfolioValueAsync(
        string userId,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates account balances with breakdown by account
    /// </summary>
    Task<List<PortfolioAccountBalanceDto>> CalculateAccountBalancesAsync(
        string userId,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates investment values with current prices
    /// </summary>
    Task<List<PortfolioInvestmentValueDto>> CalculateInvestmentValuesAsync(
        string userId,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Account balance breakdown DTO for portfolio calculations
/// </summary>
public record PortfolioAccountBalanceDto(
    string AccountName,
    string CurrencyCode,
    decimal Balance,
    decimal ConvertedBalance);

/// <summary>
/// Investment value breakdown DTO for portfolio calculations
/// </summary>
public record PortfolioInvestmentValueDto(
    string Symbol,
    string Name,
    decimal Quantity,
    decimal PurchasePrice,
    decimal CurrentPrice,
    decimal TotalValue,
    decimal GainLoss,
    decimal GainLossPercentage);
