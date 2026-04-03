using BudgetBuddy.API.VSA.Features.Investments.GetPortfolioValue;

namespace BudgetBuddy.API.VSA.Features.Investments.Services;

/// <summary>
/// Service for calculating portfolio values and investment performance
/// </summary>
public interface IPortfolioService
{
    /// <summary>
    /// Calculates total portfolio value (accounts + investments) for a user with detailed breakdown
    /// </summary>
    Task<(decimal AccountBalance, decimal InvestmentValue, List<AccountBalanceDto> AccountBreakdown, List<InvestmentValueDto> InvestmentBreakdown)> CalculatePortfolioValueAsync(
        string userId,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Calculates account balances with breakdown by account
    /// </summary>
    Task<List<AccountBalanceDto>> CalculateAccountBalancesAsync(
        string userId,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates investment values with current prices
    /// </summary>
    Task<List<InvestmentValueDto>> CalculateInvestmentValuesAsync(
        string userId,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default);
}
