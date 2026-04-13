namespace BudgetBuddy.Application.Features.Reports.GetSpendingByCategory;

public record GetSpendingByCategoryQuery(
    LocalDate? StartDate = null,
    LocalDate? EndDate = null,
    Guid? AccountId = null,
    string? DisplayCurrency = null
) : IRequest<SpendingByCategoryResponse>;

public record SpendingByCategoryResponse(
    decimal TotalSpending,
    List<CategorySpending> Categories,
    string Currency
);

public record CategorySpending(
    Guid? CategoryId,
    string CategoryName,
    decimal Amount,
    int TransactionCount,
    decimal Percentage
);
