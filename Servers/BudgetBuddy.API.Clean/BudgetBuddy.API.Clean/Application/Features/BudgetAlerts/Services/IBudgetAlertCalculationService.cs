using BudgetBuddy.Application.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.Application.Features.BudgetAlerts.Services;

public interface IBudgetAlertCalculationService
{
    Task<IReadOnlyList<BudgetAlertDto>> CalculateAlertsAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
