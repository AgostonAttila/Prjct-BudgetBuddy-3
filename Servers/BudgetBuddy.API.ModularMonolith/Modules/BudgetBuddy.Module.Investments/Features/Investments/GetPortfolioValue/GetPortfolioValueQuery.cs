namespace BudgetBuddy.Module.Investments.Features.GetPortfolioValue;

public record GetPortfolioValueQuery(
    string? TargetCurrency = null
) : IRequest<PortfolioValueResponse>;

public record PortfolioValueResponse(
    decimal TotalAccountBalance,
    decimal TotalInvestmentValue,
    decimal TotalPortfolioValue,
    string Currency,
    List<PortfolioAccountBalanceDto> AccountBreakdown,
    List<PortfolioInvestmentValueDto> InvestmentBreakdown
);

// AccountBalanceDto and InvestmentValueDto are now PortfolioAccountBalanceDto and PortfolioInvestmentValueDto
// defined in BudgetBuddy.Shared.Contracts.Investments (via GlobalUsings)
