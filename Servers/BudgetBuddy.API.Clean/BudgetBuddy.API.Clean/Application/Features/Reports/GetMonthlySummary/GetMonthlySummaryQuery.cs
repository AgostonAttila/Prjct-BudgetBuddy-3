namespace BudgetBuddy.Application.Features.Reports.GetMonthlySummary;

public record GetMonthlySummaryQuery(
    int Year,
    int Month,
    Guid? AccountId = null,
    string? DisplayCurrency = null
) : IRequest<MonthlySummaryResponse>;

public record MonthlySummaryResponse(
    int Year,
    int Month,
    string MonthName,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetIncome,
    decimal StartingBalance,
    decimal EndingBalance,
    int TotalTransactions,
    List<CategorySummary> TopCategories,
    List<DailySummary> DailyBreakdown,
    string Currency
);

public record CategorySummary(
    string CategoryName,
    decimal Amount,
    int Count
);

public record DailySummary(
    int Day,
    decimal Income,
    decimal Expense,
    decimal Net,
    int TransactionCount
);
