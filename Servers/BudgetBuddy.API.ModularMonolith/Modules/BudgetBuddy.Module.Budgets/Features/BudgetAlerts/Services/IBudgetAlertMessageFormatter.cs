using BudgetBuddy.Module.Budgets.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.Module.Budgets.Features.BudgetAlerts.Services;

public interface IBudgetAlertMessageFormatter
{
    string GenerateAlertMessage(
        AlertLevel alertLevel,
        decimal utilizationPercentage,
        decimal remaining);
}
