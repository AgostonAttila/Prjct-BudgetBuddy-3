using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Features.BudgetAlerts.Services;

namespace BudgetBuddy.Application.Features.BudgetAlerts.GetBudgetAlerts;

//TODO with Hangfire or Quartz

public class GetBudgetAlertsHandler(
    IBudgetAlertCalculationService budgetAlertService,
    ICurrentUserService currentUserService,
    IClock clock,
    ILogger<GetBudgetAlertsHandler> logger) : UserAwareHandler<GetBudgetAlertsQuery, GetBudgetAlertsResponse>(currentUserService)
{
    public override async Task<GetBudgetAlertsResponse> Handle(
        GetBudgetAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var (year, month) = GetYearAndMonth(request, clock);

        var alerts = await budgetAlertService.CalculateAlertsAsync(
            UserId,
            year,
            month,
            cancellationToken);

        var statistics = CalculateStatistics(alerts);

        logger.LogInformation(
            "Budget alerts retrieved for user {UserId}: {WarningCount} warnings, {ExceededCount} exceeded",
            UserId,
            statistics.WarningCount,
            statistics.ExceededCount);

        return new GetBudgetAlertsResponse(
            Year: year,
            Month: month,
            TotalAlerts: alerts.Count,
            WarningCount: statistics.WarningCount,
            ExceededCount: statistics.ExceededCount,
            Alerts: alerts.ToList()
        );
    }

    private static (int Year, int Month) GetYearAndMonth(GetBudgetAlertsQuery request, IClock clock)
    {
        var today = clock.GetCurrentInstant().InUtc().Date;
        var year = request.Year ?? today.Year;
        var month = request.Month ?? today.Month;
        return (year, month);
    }

    private static (int WarningCount, int ExceededCount) CalculateStatistics(IReadOnlyList<BudgetAlertDto> alerts)
    {
        var warningCount = alerts.Count(a => a.AlertLevel == AlertLevel.Warning);
        var exceededCount = alerts.Count(a => a.AlertLevel == AlertLevel.Exceeded);
        return (warningCount, exceededCount);
    }
}
