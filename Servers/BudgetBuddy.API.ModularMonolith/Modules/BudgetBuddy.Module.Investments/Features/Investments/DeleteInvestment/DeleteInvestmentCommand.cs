using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.Investments.Features.DeleteInvestment;

public record DeleteInvestmentCommand(
    Guid Id
) : IRequest<Unit>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Investments, Tags.PortfolioValue, Tags.InvestmentPerformance, Tags.Dashboard];
}
