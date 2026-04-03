using BudgetBuddy.API.VSA.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.API.VSA.Features.BudgetAlerts.Services;

public interface IBudgetAlertRuleEngine
{
    AlertLevel DetermineAlertLevel(decimal utilizationPercentage);

    decimal CalculateCategorySpending(
        IEnumerable<Transaction> transactions,
        Guid categoryId);
}
