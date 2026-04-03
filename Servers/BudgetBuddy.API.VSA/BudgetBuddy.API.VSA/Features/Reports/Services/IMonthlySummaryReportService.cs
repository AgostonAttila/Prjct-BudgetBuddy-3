using BudgetBuddy.API.VSA.Features.Reports.GetMonthlySummary;

namespace BudgetBuddy.API.VSA.Features.Reports.Services;

public interface IMonthlySummaryReportService
{
    Task<(decimal TotalIncome, decimal TotalExpense, decimal NetIncome, decimal StartingBalance, decimal EndingBalance, int TotalTransactions, List<CategorySummary> TopCategories, List<DailySummary> DailyBreakdown)>
        CalculateMonthlySummaryAsync(
            string userId,
            int year,
            int month,
            Guid? accountId = null,
            string displayCurrency = "USD",
            CancellationToken cancellationToken = default);
}
