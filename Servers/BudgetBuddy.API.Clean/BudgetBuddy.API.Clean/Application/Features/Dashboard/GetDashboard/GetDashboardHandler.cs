using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Features.Dashboard.Services;

namespace BudgetBuddy.Application.Features.Dashboard.GetDashboard;

public class GetDashboardHandler(
    IDashboardService dashboardService,
    ICurrentUserService currentUserService,
    IUserCurrencyService userCurrencyService,
    IClock clock,
    ILogger<GetDashboardHandler> logger) : UserAwareHandler<GetDashboardQuery, GetDashboardResponse>(currentUserService)
{
    public override async Task<GetDashboardResponse> Handle(
        GetDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var (currentYear, currentMonth, monthStart, monthEnd) = GetCurrentMonthInfo(clock);

        // Determine display currency: request parameter or user's default currency
        var displayCurrency = await userCurrencyService.GetDisplayCurrencyAsync(request.DisplayCurrency, cancellationToken);

        logger.LogInformation("Loading dashboard for user {UserId}, month: {Year}-{Month}, displayCurrency: {Currency}",
            UserId, currentYear, currentMonth, displayCurrency);

        // TODO: Load all dashboard components sequentially to avoid DbContext concurrency issues
        // After implementing IDbContextFactory in DashboardService (see TODO in DashboardService.cs),
        // replace this sequential code with parallel execution:
        //
        // var (accountsSummary, monthSummary, budgetSummary, topCategories, recentTransactions) = await (
        //     dashboardService.GetAccountsSummaryAsync(UserId, displayCurrency, cancellationToken),
        //     dashboardService.GetMonthSummaryAsync(UserId, currentYear, currentMonth, displayCurrency, cancellationToken),
        //     dashboardService.GetBudgetSummaryAsync(UserId, currentYear, currentMonth, displayCurrency, cancellationToken),
        //     dashboardService.GetTopCategoriesAsync(UserId, monthStart, monthEnd, 5, displayCurrency, cancellationToken),
        //     dashboardService.GetRecentTransactionsAsync(UserId, 10, cancellationToken)
        // );
        var accountsSummary = await dashboardService.GetAccountsSummaryAsync(UserId, displayCurrency, cancellationToken);
        var monthSummary = await dashboardService.GetMonthSummaryAsync(UserId, currentYear, currentMonth, displayCurrency, cancellationToken);
        var budgetSummary = await dashboardService.GetBudgetSummaryAsync(UserId, currentYear, currentMonth, displayCurrency, cancellationToken);
        var topCategories = await dashboardService.GetTopCategoriesAsync(UserId, monthStart, monthEnd, 5, displayCurrency, cancellationToken);
        var recentTransactions = await dashboardService.GetRecentTransactionsAsync(UserId, 10, cancellationToken);

        logger.LogInformation("Dashboard loaded successfully for user {UserId}", UserId);

        return new GetDashboardResponse(
            DisplayCurrency: displayCurrency,
            AccountsSummary: accountsSummary,
            CurrentMonthSummary: monthSummary,
            BudgetSummary: budgetSummary,
            TopCategories: topCategories.ToList(),
            RecentTransactions: recentTransactions.ToList()
        );
    }

    private static (int Year, int Month, LocalDate MonthStart, LocalDate MonthEnd) GetCurrentMonthInfo(IClock clock)
    {
        var (year, month, _) = clock.GetCurrentInstant().InUtc().Date;
        var monthStart = new LocalDate(year, month, 1);
        var monthEnd = monthStart.PlusMonths(1).PlusDays(-1);

        return (year,  month, monthStart, monthEnd);
    }
}
