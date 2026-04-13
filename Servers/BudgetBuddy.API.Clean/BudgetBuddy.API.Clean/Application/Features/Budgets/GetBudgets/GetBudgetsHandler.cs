using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Budgets.GetBudgets;

public class GetBudgetsHandler(
    IBudgetRepository budgetRepo,
    ICurrentUserService currentUserService,
    ILogger<GetBudgetsHandler> logger) : UserAwareHandler<GetBudgetsQuery, List<BudgetDto>>(currentUserService)
{
    public override async Task<List<BudgetDto>> Handle(
        GetBudgetsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching budgets for user {UserId}", UserId);

        var budgets = await budgetRepo.GetFilteredAsync(
            UserId, request.Year, request.Month, request.CategoryId, cancellationToken);

        var budgetDtos = budgets
            .Select(b => new BudgetDto(b.Id, b.Name, b.CategoryId, b.Category.Name, b.Amount, b.CurrencyCode, b.Year, b.Month))
            .ToList();

        logger.LogInformation("Found {Count} budgets for user {UserId}", budgetDtos.Count, UserId);

        return budgetDtos;
    }
}
