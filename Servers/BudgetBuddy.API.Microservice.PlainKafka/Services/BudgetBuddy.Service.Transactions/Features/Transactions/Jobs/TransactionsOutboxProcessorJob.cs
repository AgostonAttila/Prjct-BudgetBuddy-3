using System.Text.Json;
using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using BudgetBuddy.Shared.Messages.Integration;
using BudgetBuddy.Shared.Messages.Topics;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Quartz;

namespace BudgetBuddy.Service.Transactions.Features.Transactions.Jobs;

/// <summary>
/// Reads unprocessed OutboxMessages from the transactions DB and publishes them
/// to the appropriate Kafka topics. Runs on a short cron interval (e.g. every 30 seconds).
/// Provides at-least-once delivery guarantee — consumers must be idempotent.
/// </summary>
public class TransactionsOutboxProcessorJob(
    IServiceScopeFactory scopeFactory,
    ILogger<TransactionsOutboxProcessorJob> logger) : ScheduledJobBase(logger)
{
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions { WriteIndented = false }
            .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);


    // Compile-time safe registry: short event name → (CLR Type, Kafka topic).
    // No reflection-based Type.GetType() — survives namespace/assembly renames.
    private static readonly Dictionary<string, (Type Type, string Topic)> EventRegistry = new()
    {
        [nameof(TransactionCreatedEvent)]  = (typeof(TransactionCreatedEvent),  TopicNames.TransactionsCreated),
        [nameof(TransactionUpdatedEvent)]  = (typeof(TransactionUpdatedEvent),  TopicNames.TransactionsUpdated),
        [nameof(TransactionDeletedEvent)]  = (typeof(TransactionDeletedEvent),  TopicNames.TransactionsDeleted),
        [nameof(TransactionReversedEvent)] = (typeof(TransactionReversedEvent), TopicNames.TransactionsReversed),
    };

    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetBuddy.Service.Transactions.Persistence.TransactionsDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(context.CancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        Logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, publisher, clock, context.CancellationToken);
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IEventPublisher publisher,
        IClock clock,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!EventRegistry.TryGetValue(message.EventType, out var entry))
            {
                Logger.LogError("Unknown event type in outbox: {EventType}", message.EventType);
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

            await publisher.PublishAsync(integrationEvent, entry.Type, entry.Topic, cancellationToken);

            message.ProcessedAt = clock.GetCurrentInstant();
            message.Error = null;

            Logger.LogDebug("Published outbox message {MessageId} to {Topic}", message.Id, entry.Topic);
        }
        catch (Exception ex)
        {
            message.RetryCount++;
            message.Error = ex.Message;
            Logger.LogWarning(ex, "Failed to publish outbox message {MessageId} (attempt {Attempt})", message.Id, message.RetryCount);

            if (message.RetryCount >= MaxRetries)
            {
                await SendToDlqAsync(message, publisher, clock, cancellationToken);
            }
        }
    }

    private async Task SendToDlqAsync(
        OutboxMessage message,
        IEventPublisher publisher,
        IClock clock,
        CancellationToken ct)
    {
        try
        {
            var dlqPayload = new
            {
                OriginalMessageId = message.Id,
                message.EventType,
                message.Payload,
                Error = message.Error ?? "Unknown error",
                message.RetryCount,
                FailedAt = clock.GetCurrentInstant().ToDateTimeUtc(),
                SourceService = "transactions-service",
            };

            await publisher.PublishStateAsync(TopicNames.TransactionsDlq, message.Id.ToString(), dlqPayload, ct);

            message.ProcessedAt = clock.GetCurrentInstant();

            KafkaMetrics.MessagesSentToDlq.Add(1,
                new KeyValuePair<string, object?>("service", "transactions"),
                new KeyValuePair<string, object?>("event_type", message.EventType));

            Logger.LogCritical(
                "Outbox message {MessageId} ({EventType}) moved to DLQ after {RetryCount} retries. Error: {Error}",
                message.Id, message.EventType, message.RetryCount, message.Error);
        }
        catch (Exception dlqEx)
        {
            Logger.LogCritical(dlqEx,
                "CRITICAL: Failed to send outbox message {MessageId} to DLQ. Manual intervention required.",
                message.Id);
        }
    }

}
