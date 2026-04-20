using BudgetBuddy.Module.Budgets.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.Module.Budgets.Features.BudgetAlerts.Services;

public interface IBudgetAlertCalculationService
{
    Task<IReadOnlyList<BudgetAlertDto>> CalculateAlertsAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
