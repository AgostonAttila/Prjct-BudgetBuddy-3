namespace BudgetBuddy.Application.Features.Reports.GetIncomeVsExpense;

public record GetIncomeVsExpenseQuery(
    LocalDate? StartDate = null,
    LocalDate? EndDate = null,
    Guid? AccountId = null,
    string? DisplayCurrency = null
) : IRequest<IncomeVsExpenseResponse>;

public record IncomeVsExpenseResponse(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetIncome,
    int IncomeTransactionCount,
    int ExpenseTransactionCount,
    List<MonthlyBreakdown> MonthlyData,
    string Currency
);

public record MonthlyBreakdown(
    int Year,
    int Month,
    string MonthName,
    decimal Income,
    decimal Expense,
    decimal Net
);
