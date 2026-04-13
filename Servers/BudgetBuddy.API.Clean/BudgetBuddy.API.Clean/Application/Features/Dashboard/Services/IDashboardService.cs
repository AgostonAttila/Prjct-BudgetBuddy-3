using BudgetBuddy.Application.Features.Dashboard.GetDashboard;
using NodaTime;

namespace BudgetBuddy.Application.Features.Dashboard.Services;

/// <summary>
/// Service for calculating dashboard statistics and summaries
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Calculates total account balances for a user
    /// </summary>
    Task<DashboardAccountsSummary> GetAccountsSummaryAsync(
        string userId,
        string displayCurrency = "USD",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates income/expense summary for current month
    /// </summary>
    Task<DashboardMonthSummary> GetMonthSummaryAsync(
        string userId,
        int year,
        int month,
        string displayCurrency = "USD",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates budget utilization for current month
    /// </summary>
    Task<DashboardBudgetSummary> GetBudgetSummaryAsync(
        string userId,
        int year,
        int month,
        string displayCurrency = "USD",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top spending categories for a given period
    /// </summary>
    Task<IReadOnlyList<DashboardTopCategory>> GetTopCategoriesAsync(
        string userId,
        LocalDate startDate,
        LocalDate endDate,
        int topCount = 5,
        string displayCurrency = "USD",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent transactions for a user
    /// </summary>
    Task<IReadOnlyList<DashboardRecentTransaction>> GetRecentTransactionsAsync(
        string userId,
        int count = 10,
        CancellationToken cancellationToken = default);
}
