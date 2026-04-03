using BudgetBuddy.API.VSA.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.API.VSA.Features.BudgetAlerts.Services;

public interface IBudgetAlertMessageFormatter
{
    string GenerateAlertMessage(
        AlertLevel alertLevel,
        decimal utilizationPercentage,
        decimal remaining);
}
