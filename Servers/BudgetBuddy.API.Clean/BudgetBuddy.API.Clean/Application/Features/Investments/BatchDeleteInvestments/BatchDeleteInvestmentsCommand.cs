using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Domain.Constants;

namespace BudgetBuddy.Application.Features.Investments.BatchDeleteInvestments;

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
