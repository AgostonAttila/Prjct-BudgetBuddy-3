namespace BudgetBuddy.Shared.Messages.Contracts.Investments;

public interface IPortfolioService
{
    Task<(decimal AccountBalance, decimal InvestmentValue, List<PortfolioAccountBalanceDto> AccountBreakdown, List<PortfolioInvestmentValueDto> InvestmentBreakdown)> CalculatePortfolioValueAsync(
        string userId,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default);

    Task<List<PortfolioAccountBalanceDto>> CalculateAccountBalancesAsync(
        string userId,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default);

    Task<List<PortfolioInvestmentValueDto>> CalculateInvestmentValuesAsync(
        string userId,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default);
}

public record PortfolioAccountBalanceDto(
    string AccountName,
    string CurrencyCode,
    decimal Balance,
    decimal ConvertedBalance);

public record PortfolioInvestmentValueDto(
    string Symbol,
    string Name,
    decimal Quantity,
    decimal PurchasePrice,
    decimal CurrentPrice,
    decimal TotalValue,
    decimal GainLoss,
    decimal GainLossPercentage);
