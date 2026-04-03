using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Features.Reports.Services;

namespace BudgetBuddy.API.VSA.Features.Reports.GetIncomeVsExpense;

public class GetIncomeVsExpenseHandler(
    IIncomeExpenseReportService reportService,
    ICurrentUserService currentUserService,
    IUserCurrencyService userCurrencyService,
    ILogger<GetIncomeVsExpenseHandler> logger)
    : ReportHandlerBase<GetIncomeVsExpenseQuery, IncomeVsExpenseResponse>(
        currentUserService, userCurrencyService, logger)
{
    protected override string? GetDisplayCurrency(GetIncomeVsExpenseQuery request) => request.DisplayCurrency;

    protected override async Task<IncomeVsExpenseResponse> HandleCoreAsync(
        string userId,
        string displayCurrency,
        GetIncomeVsExpenseQuery request,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Getting income vs expense for user {UserId}, period: {StartDate} - {EndDate}, currency: {Currency}",
            userId, request.StartDate, request.EndDate, displayCurrency);

        var (totalIncome, totalExpense, netIncome, incomeCount, expenseCount) =
            await reportService.CalculateIncomeVsExpenseAsync(
                userId, request.StartDate, request.EndDate, request.AccountId, displayCurrency, cancellationToken);

        var monthlyData = await reportService.GetMonthlyBreakdownAsync(
            userId, request.StartDate, request.EndDate, request.AccountId, displayCurrency, cancellationToken);

        Logger.LogInformation(
            "Income vs expense calculated: Income={Income}, Expense={Expense}, Net={Net}",
            totalIncome, totalExpense, netIncome);

        return new IncomeVsExpenseResponse(
            TotalIncome: totalIncome,
            TotalExpense: totalExpense,
            NetIncome: netIncome,
            IncomeTransactionCount: incomeCount,
            ExpenseTransactionCount: expenseCount,
            MonthlyData: monthlyData,
            Currency: displayCurrency
        );
    }
}
