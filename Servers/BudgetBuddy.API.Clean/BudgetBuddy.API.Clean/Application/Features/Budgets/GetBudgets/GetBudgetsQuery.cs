namespace BudgetBuddy.Application.Features.Budgets.GetBudgets;

public record GetBudgetsQuery(
    int? Year = null,
    int? Month = null,
    Guid? CategoryId = null
) : IRequest<List<BudgetDto>>;

public record BudgetDto(
    Guid Id,
    string Name,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string CurrencyCode,
    int Year,
    int Month
);
