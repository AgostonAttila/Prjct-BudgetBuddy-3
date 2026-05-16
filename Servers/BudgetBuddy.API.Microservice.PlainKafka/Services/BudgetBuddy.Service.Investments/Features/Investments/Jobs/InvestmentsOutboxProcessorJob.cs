using System.Text.Json;
using BudgetBuddy.Service.Investments.Persistence;
using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Messages.Events.Investments;
using BudgetBuddy.Shared.Messages.Integration;
using BudgetBuddy.Shared.Messages.Topics;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Quartz;

namespace BudgetBuddy.Service.Investments.Jobs;

public class InvestmentsOutboxProcessorJob(
    IServiceScopeFactory scopeFactory,
    ILogger<InvestmentsOutboxProcessorJob> logger) : ScheduledJobBase(logger)
{
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions { WriteIndented = false }
            .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

    private static readonly Dictionary<string, (Type Type, string Topic)> EventRegistry = new()
    {
        [nameof(InvestmentCreatedEvent)]   = (typeof(InvestmentCreatedEvent),   TopicNames.InvestmentsCreated),
        [nameof(InvestmentUpdatedEvent)]   = (typeof(InvestmentUpdatedEvent),   TopicNames.InvestmentsUpdated),
        [nameof(InvestmentDeletedEvent)]   = (typeof(InvestmentDeletedEvent),   TopicNames.InvestmentsDeleted),
        [nameof(MarketPriceUpdatedEvent)]  = (typeof(MarketPriceUpdatedEvent),  TopicNames.MarketPriceUpdated),
    };

    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db        = scope.ServiceProvider.GetRequiredService<InvestmentsDbContext>();
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

        Logger.LogInformation("Processing {Count} investments outbox messages", messages.Count);

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
                Logger.LogError("Unknown event type in investments outbox: {EventType}", message.EventType);
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
            Logger.LogWarning(ex, "Failed to publish investments outbox message {MessageId} (attempt {Attempt})", message.Id, message.RetryCount);

            if (message.RetryCount >= MaxRetries)
            {
                Logger.LogCritical(
                    "Investments outbox message {MessageId} ({EventType}) exceeded max retries. Error: {Error}",
                    message.Id, message.EventType, ex.Message);
                message.ProcessedAt = clock.GetCurrentInstant();
            }
        }
    }
}
