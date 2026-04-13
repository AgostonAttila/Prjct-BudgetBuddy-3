using BudgetBuddy.Application.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.Application.Features.BudgetAlerts.Services;

public interface IBudgetAlertRuleEngine
{
    AlertLevel DetermineAlertLevel(decimal utilizationPercentage);

    decimal CalculateCategorySpending(
        IEnumerable<Transaction> transactions,
        Guid categoryId);
}
