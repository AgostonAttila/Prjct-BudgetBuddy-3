using System.Text.Json;
using BudgetBuddy.Service.Budgets.Persistence;
using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Messages.Events.Budgets;
using BudgetBuddy.Shared.Messages.Integration;
using BudgetBuddy.Shared.Messages.Topics;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Quartz;

namespace BudgetBuddy.Service.Budgets.Jobs;

public class BudgetsOutboxProcessorJob(
    IServiceScopeFactory scopeFactory,
    ILogger<BudgetsOutboxProcessorJob> logger) : ScheduledJobBase(logger)
{
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private static readonly Dictionary<string, (Type Type, string Topic)> EventRegistry = new()
    {
        [nameof(BudgetCreatedEvent)]      = (typeof(BudgetCreatedEvent),      TopicNames.BudgetsCreated),
        [nameof(BudgetUpdatedEvent)]      = (typeof(BudgetUpdatedEvent),      TopicNames.BudgetsUpdated),
        [nameof(BudgetDeletedEvent)]      = (typeof(BudgetDeletedEvent),      TopicNames.BudgetsDeleted),
        [nameof(BudgetAlertTriggeredEvent)] = (typeof(BudgetAlertTriggeredEvent), TopicNames.BudgetAlertTriggered),
    };

    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db        = scope.ServiceProvider.GetRequiredService<BudgetsDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var clock     = scope.ServiceProvider.GetRequiredService<IClock>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(context.CancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        Logger.LogInformation("Processing {Count} budgets outbox messages", messages.Count);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, publisher, clock, context.CancellationToken);
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    private async Task ProcessMessageAsync(OutboxMessage message, IEventPublisher publisher, IClock clock, CancellationToken ct)
    {
        try
        {
            if (!EventRegistry.TryGetValue(message.EventType, out var entry))
            {
                Logger.LogError("Unknown event type in budgets outbox: {EventType}", message.EventType);
                message.Error = $"Unknown event type: {message.EventType}";
                message.ProcessedAt = clock.GetCurrentInstant();
                return;
            }

            var integrationEvent = (IIntegrationEvent?)JsonSerializer.Deserialize(message.Payload, entry.Type, JsonOptions);
            if (integrationEvent is null)
            {
                message.Error = "Failed to deserialize payload";
                message.ProcessedAt = clock.GetCurrentInstant();
                return;
            }

            await publisher.PublishAsync(integrationEvent, entry.Type, entry.Topic, ct);
            message.ProcessedAt = clock.GetCurrentInstant();
            message.Error = null;
        }
        catch (Exception ex)
        {
            message.RetryCount++;
            message.Error = ex.Message;
            Logger.LogWarning(ex, "Failed to publish budgets outbox message {MessageId} (attempt {Attempt})", message.Id, message.RetryCount);

            if (message.RetryCount >= MaxRetries)
            {
                Logger.LogCritical(
                    "Budgets outbox message {MessageId} ({EventType}) exceeded max retries. Error: {Error}",
                    message.Id, message.EventType, ex.Message);
                message.ProcessedAt = clock.GetCurrentInstant();
            }
        }
    }
}
