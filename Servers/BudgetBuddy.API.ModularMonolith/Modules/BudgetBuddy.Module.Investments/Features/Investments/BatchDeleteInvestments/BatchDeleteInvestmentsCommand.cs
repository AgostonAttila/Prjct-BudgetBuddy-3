using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.Investments.Features.BatchDeleteInvestments;

public record BatchDeleteInvestmentsCommand(List<Guid> InvestmentIds) : IRequest<BatchDeleteInvestmentsResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Investments, Tags.PortfolioValue, Tags.InvestmentPerformance, Tags.Dashboard];
}

public record BatchDeleteInvestmentsResponse(
    int TotalRequested,
    int SuccessCount,
    int FailedCount,
    List<string> Errors
);
