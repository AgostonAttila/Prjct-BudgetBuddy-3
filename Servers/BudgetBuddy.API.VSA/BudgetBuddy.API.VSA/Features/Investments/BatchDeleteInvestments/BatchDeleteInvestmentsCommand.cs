using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Domain.Constants;

namespace BudgetBuddy.API.VSA.Features.Investments.BatchDeleteInvestments;

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
