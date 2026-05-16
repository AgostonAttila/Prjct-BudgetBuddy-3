using BudgetBuddy.Service.Analytics.Features.Dashboard.Services;
using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Financial;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Service.Analytics.Features.Dashboard.GetDashboard;

public class GetDashboardHandler(
    IServiceScopeFactory scopeFactory,
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

        // Capture UserId before entering parallel tasks (string copy, not closure over mutable state)
        var userId = UserId;

        logger.LogInformation("Loading dashboard for user {UserId}, month: {Year}-{Month}, displayCurrency: {Currency}",
            userId, currentYear, currentMonth, displayCurrency);

        // Each task runs in its own child scope → its own AnalyticsDbContext instance → no EF Core
        // concurrency exception. HybridCache (singleton) is shared across scopes, so stampede
        // protection and cache hits still work correctly.
        var accountsSummaryTask   = RunInScopeAsync(sp => sp.GetRequiredService<IDashboardService>()
            .GetAccountsSummaryAsync(userId, displayCurrency, cancellationToken));
        var monthSummaryTask      = RunInScopeAsync(sp => sp.GetRequiredService<IDashboardService>()
            .GetMonthSummaryAsync(userId, currentYear, currentMonth, displayCurrency, cancellationToken));
        var budgetSummaryTask     = RunInScopeAsync(sp => sp.GetRequiredService<IDashboardService>()
            .GetBudgetSummaryAsync(userId, currentYear, currentMonth, displayCurrency, cancellationToken));
        var topCategoriesTask     = RunInScopeAsync(sp => sp.GetRequiredService<IDashboardService>()
            .GetTopCategoriesAsync(userId, monthStart, monthEnd, 5, displayCurrency, cancellationToken));
        var recentTransactionsTask = RunInScopeAsync(sp => sp.GetRequiredService<IDashboardService>()
            .GetRecentTransactionsAsync(userId, 10, cancellationToken));

        await Task.WhenAll(accountsSummaryTask, monthSummaryTask, budgetSummaryTask, topCategoriesTask, recentTransactionsTask);

        var accountsSummary    = accountsSummaryTask.Result;
        var monthSummary       = monthSummaryTask.Result;
        var budgetSummary      = budgetSummaryTask.Result;
        var topCategories      = topCategoriesTask.Result;
        var recentTransactions = recentTransactionsTask.Result;

        logger.LogInformation("Dashboard loaded successfully for user {UserId}", userId);

        return new GetDashboardResponse(
            DisplayCurrency: displayCurrency,
            AccountsSummary: accountsSummary,
            CurrentMonthSummary: monthSummary,
            BudgetSummary: budgetSummary,
            TopCategories: topCategories.ToList(),
            RecentTransactions: recentTransactions.ToList()
        );
    }

    private async Task<T> RunInScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    private static (int Year, int Month, LocalDate MonthStart, LocalDate MonthEnd) GetCurrentMonthInfo(IClock clock)
    {
        var (year, month, _) = clock.GetCurrentInstant().InUtc().Date;
        var monthStart = new LocalDate(year, month, 1);
        var monthEnd = monthStart.PlusMonths(1).PlusDays(-1);

        return (year, month, monthStart, monthEnd);
    }
}
