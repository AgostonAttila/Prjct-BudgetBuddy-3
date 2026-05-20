using BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.Service.Budgets.Features.BudgetAlerts.Services;

public interface IBudgetAlertRuleEngine
{
    AlertLevel DetermineAlertLevel(decimal utilizationPercentage);
}
