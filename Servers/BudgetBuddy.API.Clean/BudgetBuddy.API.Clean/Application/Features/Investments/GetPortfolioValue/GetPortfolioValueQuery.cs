namespace BudgetBuddy.Application.Features.Investments.GetPortfolioValue;

public record GetPortfolioValueQuery(
    string? TargetCurrency = null
) : IRequest<PortfolioValueResponse>;

public record PortfolioValueResponse(
    decimal TotalAccountBalance,
    decimal TotalInvestmentValue,
    decimal TotalPortfolioValue,
    string Currency,
    List<AccountBalanceDto> AccountBreakdown,
    List<InvestmentValueDto> InvestmentBreakdown
);

public record AccountBalanceDto(
    string AccountName,
    string CurrencyCode,
    decimal Balance,
    decimal ConvertedBalance
);

public record InvestmentValueDto(
    string Symbol,
    string Name,
    decimal Quantity,
    decimal PurchasePrice,
    decimal CurrentPrice,
    decimal TotalValue,
    decimal GainLoss,
    decimal GainLossPercentage
);
