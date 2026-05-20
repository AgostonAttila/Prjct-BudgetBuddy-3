using BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.Service.Budgets.Features.BudgetAlerts.Services;

public interface IBudgetAlertCalculationService
{
    Task<IReadOnlyList<BudgetAlertDto>> CalculateAlertsAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
