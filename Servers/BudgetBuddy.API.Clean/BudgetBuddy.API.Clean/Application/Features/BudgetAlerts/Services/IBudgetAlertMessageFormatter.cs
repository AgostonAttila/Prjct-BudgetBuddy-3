using BudgetBuddy.Application.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.Application.Features.BudgetAlerts.Services;

public interface IBudgetAlertMessageFormatter
{
    string GenerateAlertMessage(
        AlertLevel alertLevel,
        decimal utilizationPercentage,
        decimal remaining);
}
