namespace BudgetBuddy.Module.Budgets.Features.GetBudgets;

public record GetBudgetsQuery(
    int? Year = null,
    int? Month = null,
    Guid? CategoryId = null,
    int PageNumber = 1,
    int PageSize = 50
) : IRequest<GetBudgetsResponse>;

public record GetBudgetsResponse(
    List<BudgetDto> Budgets,
    int TotalCount,
    int PageNumber,
    int PageSize
);

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
