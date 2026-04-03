using BudgetBuddy.API.VSA.Features.Reports.Services;
using System.Globalization;
using BudgetBuddy.API.VSA.Common.Infrastructure;

namespace BudgetBuddy.API.VSA.Features.Reports.GetMonthlySummary;

public class GetMonthlySummaryHandler(
    IMonthlySummaryReportService reportService,
    ICurrentUserService currentUserService,
    IUserCurrencyService userCurrencyService,
    ILogger<GetMonthlySummaryHandler> logger)
    : ReportHandlerBase<GetMonthlySummaryQuery, MonthlySummaryResponse>(
        currentUserService, userCurrencyService, logger)
{
    protected override string? GetDisplayCurrency(GetMonthlySummaryQuery request) => request.DisplayCurrency;

    protected override async Task<MonthlySummaryResponse> HandleCoreAsync(
        string userId,
        string displayCurrency,
        GetMonthlySummaryQuery request,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Getting monthly summary for user {UserId}, year {Year}, month {Month}, currency: {Currency}",
            userId, request.Year, request.Month, displayCurrency);

        var (totalIncome, totalExpense, netIncome, startingBalance, endingBalance, totalTransactions, topCategories, dailyBreakdown) =
            await reportService.CalculateMonthlySummaryAsync(
                userId, request.Year, request.Month, request.AccountId, displayCurrency, cancellationToken);

        Logger.LogInformation(
            "Monthly summary calculated: Income={Income}, Expense={Expense}, Net={Net}, Transactions={Count}",
            totalIncome, totalExpense, netIncome, totalTransactions);

        return new MonthlySummaryResponse(
            request.Year,
            request.Month,
            CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(request.Month),
            totalIncome,
            totalExpense,
            netIncome,
            startingBalance,
            endingBalance,
            totalTransactions,
            topCategories,
            dailyBreakdown,
            displayCurrency
        );
    }
}
