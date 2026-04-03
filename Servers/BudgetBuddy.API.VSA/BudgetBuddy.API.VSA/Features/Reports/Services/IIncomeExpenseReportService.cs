using BudgetBuddy.API.VSA.Features.Reports.GetIncomeVsExpense;

namespace BudgetBuddy.API.VSA.Features.Reports.Services;

public interface IIncomeExpenseReportService
{
    Task<(decimal TotalIncome, decimal TotalExpense, decimal NetIncome, int IncomeCount, int ExpenseCount)>
        CalculateIncomeVsExpenseAsync(
            string userId,
            LocalDate? startDate = null,
            LocalDate? endDate = null,
            Guid? accountId = null,
            string displayCurrency = "USD",
            CancellationToken cancellationToken = default);

    Task<List<MonthlyBreakdown>> GetMonthlyBreakdownAsync(
        string userId,
        LocalDate? startDate = null,
        LocalDate? endDate = null,
        Guid? accountId = null,
        string displayCurrency = "USD",
        CancellationToken cancellationToken = default);
}
