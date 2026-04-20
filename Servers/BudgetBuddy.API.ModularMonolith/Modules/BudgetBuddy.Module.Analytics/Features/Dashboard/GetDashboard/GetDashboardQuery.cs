namespace BudgetBuddy.Module.Analytics.Features.Dashboard.GetDashboard;

public record GetDashboardQuery(
    string? DisplayCurrency = null  // Optional: Currency for displaying converted amounts (defaults to user's default currency)
) : IRequest<GetDashboardResponse>;

public record GetDashboardResponse(
    string DisplayCurrency,  // Currency used for display/conversion
    DashboardAccountsSummary AccountsSummary,
    DashboardMonthSummary CurrentMonthSummary,
    DashboardBudgetSummary BudgetSummary,
    List<DashboardTopCategory> TopCategories,
    List<DashboardRecentTransaction> RecentTransactions
);

public record DashboardAccountsSummary(
    decimal TotalBalance,  // Converted to DisplayCurrency
    int AccountCount,
    string PrimaryCurrency  // User's default currency (informational)
);

public record DashboardMonthSummary(
    int Year,
    int Month,
    string MonthName,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetIncome,
    int TransactionCount
);

public record DashboardBudgetSummary(
    decimal TotalBudget,
    decimal TotalSpent,
    decimal Remaining,
    decimal UtilizationPercentage,
    int OverBudgetCount,
    int TotalBudgetCount
);

public record DashboardTopCategory(
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    decimal Amount,
    int TransactionCount,
    decimal Percentage
);

// Cache-safe DTO - excludes PII fields (Note, Payee) for security
public record DashboardRecentTransaction(
    Guid Id,
    string Date,
    string AccountName,
    string CategoryName,
    string Type,
    decimal Amount,
    string CurrencyCode
    // ⚠️ Note and Payee excluded - PII data should not be cached
);
