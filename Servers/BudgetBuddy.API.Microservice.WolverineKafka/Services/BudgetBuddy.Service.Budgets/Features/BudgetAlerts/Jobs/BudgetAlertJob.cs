using BudgetBuddy.Service.Budgets.Features.BudgetAlerts.Services;
using BudgetBuddy.Service.Budgets.Persistence;
using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Shared.Messages.Events.Budgets;
using NodaTime;
using Quartz;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Budgets.Jobs;

/// <summary>
/// Checks all active budgets and publishes BudgetAlertTriggeredEvent via the Wolverine
/// transactional outbox for each Warning/Exceeded budget.
/// </summary>
public class BudgetAlertJob(
    IServiceScopeFactory scopeFactory,
    ILogger<BudgetAlertJob> logger)
    : ScheduledJobBase(logger)
{
    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db           = scope.ServiceProvider.GetRequiredService<BudgetsDbContext>();
        var alertService = scope.ServiceProvider.GetRequiredService<IBudgetAlertCalculationService>();
        var clock        = scope.ServiceProvider.GetRequiredService<IClock>();

        var today = clock.GetCurrentInstant().InUtc().Date;

        var userIds = await db.Budgets
            .Where(b => b.Year == today.Year && b.Month == today.Month)
            .Select(b => b.UserId)
            .Distinct()
            .ToListAsync(context.CancellationToken);

        if (userIds.Count == 0)
        {
            return;
        }

        Logger.LogInformation(
            "Checking budget alerts for {UserCount} users ({Year}-{Month:D2})",
            userIds.Count, today.Year, today.Month);

        var outbox = scope.ServiceProvider.GetRequiredService<IDbContextOutbox>();
        outbox.Enroll(db);

        var alertCount = 0;

        foreach (var userId in userIds)
        {
            var alerts = await alertService.CalculateAlertsAsync(
                userId, today.Year, today.Month, context.CancellationToken);

            foreach (var alert in alerts)
            {
                await outbox.SendAsync(new BudgetAlertTriggeredEvent
                {
                    BudgetId         = alert.BudgetId,
                    UserId           = userId,
                    BudgetName       = alert.CategoryName,
                    Limit            = alert.BudgetAmount,
                    CurrentSpending  = alert.ActualAmount,
                    ThresholdPercent = alert.UtilizationPercentage,
                    UserEmail        = null
                });
                alertCount++;
            }
        }

        if (alertCount == 0)
        {
            Logger.LogInformation(
                "No budget alerts triggered for {Year}-{Month:D2}", today.Year, today.Month);
            return;
        }

        await outbox.SaveChangesAndFlushMessagesAsync(context.CancellationToken);

        Logger.LogInformation(
            "Queued {Count} budget alert events for {Year}-{Month:D2}",
            alertCount, today.Year, today.Month);
    }
}
