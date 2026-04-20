using BudgetBuddy.Module.Budgets.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.Module.Budgets.Features.BudgetAlerts.Services;

public interface IBudgetAlertRuleEngine
{
    AlertLevel DetermineAlertLevel(decimal utilizationPercentage);
}
