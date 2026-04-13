namespace BudgetBuddy.Application.Features.BudgetAlerts.GetBudgetAlerts;

public record GetBudgetAlertsQuery(
    int? Year,
    int? Month
) : IRequest<GetBudgetAlertsResponse>;

public record GetBudgetAlertsResponse(
    int Year,
    int Month,
    int TotalAlerts,
    int WarningCount,
    int ExceededCount,
    List<BudgetAlertDto> Alerts
);

public record BudgetAlertDto(
    Guid BudgetId,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    decimal BudgetAmount,
    decimal ActualAmount,
    decimal Remaining,
    decimal UtilizationPercentage,
    AlertLevel AlertLevel,
    string Message
);

public enum AlertLevel
{
    Safe,        // 0-79%
    Warning,     // 80-99%
    Exceeded     // 100%+
}
