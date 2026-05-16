using BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.Service.Budgets.Features.BudgetAlerts.Services;

public interface IBudgetAlertMessageFormatter
{
    string GenerateAlertMessage(
        AlertLevel alertLevel,
        decimal utilizationPercentage,
        decimal remaining);
}
