using System.Text.Json;
using BudgetBuddy.Service.Budgets.Features.BudgetAlerts.Services;
using BudgetBuddy.Service.Budgets.Persistence;
using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Messages.Events.Budgets;
using NodaTime;
using Quartz;

namespace BudgetBuddy.Service.Budgets.Jobs;

/// <summary>
/// Checks all active budgets and publishes BudgetAlertTriggeredEvent via outbox
/// for each Warning/Exceeded budget. Runs daily via Quartz scheduler
/// (configurable via BackgroundJobs:BudgetAlerts:CronExpression).
/// </summary>
public class BudgetAlertJob(
    IServiceScopeFactory scopeFactory,
    ILogger<BudgetAlertJob> logger)
    : ScheduledJobBase(logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db           = scope.ServiceProvider.GetRequiredService<BudgetsDbContext>();
        var alertService = scope.ServiceProvider.GetRequiredService<IBudgetAlertCalculationService>();
        var clock        = scope.ServiceProvider.GetRequiredService<IClock>();

        var today = clock.GetCurrentInstant().InUtc().Date;
        var now   = clock.GetCurrentInstant();

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

        var outboxMessages = new List<OutboxMessage>();

        foreach (var userId in userIds)
        {
            var alerts = await alertService.CalculateAlertsAsync(
                userId, today.Year, today.Month, context.CancellationToken);

            foreach (var alert in alerts)
            {
                var evt = new BudgetAlertTriggeredEvent
                {
                    BudgetId         = alert.BudgetId,
                    UserId           = userId,
                    BudgetName       = alert.CategoryName,
                    Limit            = alert.BudgetAmount,
                    CurrentSpending  = alert.ActualAmount,
                    ThresholdPercent = alert.UtilizationPercentage,
                    // UserEmail is not available in this service — the Notifications consumer
                    // skips sending if null. Wire up a user profile service here when available.
                    UserEmail        = null
                };

                outboxMessages.Add(new OutboxMessage
                {
                    Id        = evt.MessageId,
                    EventType = nameof(BudgetAlertTriggeredEvent),
                    Payload   = JsonSerializer.Serialize(evt, JsonOptions),
                    CreatedAt = now
                });
            }
        }

        if (outboxMessages.Count == 0)
        {
            Logger.LogInformation(
                "No budget alerts triggered for {Year}-{Month:D2}", today.Year, today.Month);
            return;
        }

        await db.OutboxMessages.AddRangeAsync(outboxMessages, context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);

        Logger.LogInformation(
            "Queued {Count} budget alert events for {Year}-{Month:D2}",
            outboxMessages.Count, today.Year, today.Month);
    }
}
