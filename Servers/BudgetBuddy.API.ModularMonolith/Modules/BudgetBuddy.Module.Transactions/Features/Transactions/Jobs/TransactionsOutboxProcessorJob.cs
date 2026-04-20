using System.Text.Json;
using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Shared.Kernel.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using Quartz;

namespace BudgetBuddy.Module.Transactions.Features.Transactions.Jobs;

/// <summary>
/// Reads unprocessed OutboxMessages from the transactions schema and publishes them
/// via MediatR so that other modules (e.g. Analytics) can react without tight coupling.
/// Runs on a short cron interval (e.g. every 30 seconds).
/// </summary>
public class TransactionsOutboxProcessorJob(
    IServiceScopeFactory scopeFactory,
    ILogger<TransactionsOutboxProcessorJob> logger) : ScheduledJobBase(logger)
{
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetBuddy.Module.Transactions.Persistence.TransactionsDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(context.CancellationToken);

        if (messages.Count == 0) return;

        Logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, publisher, clock, context.CancellationToken);
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    private static async Task ProcessMessageAsync(
        OutboxMessage message,
        IPublisher publisher,
        IClock clock,
        CancellationToken cancellationToken)
    {
        try
        {
            var type = Type.GetType(message.EventType);
            if (type is null)
            {
                message.Error = $"Unknown event type: {message.EventType}";
                message.ProcessedAt = clock.GetCurrentInstant();
                return;
            }

            var domainEvent = JsonSerializer.Deserialize(message.Payload, type, JsonOptions);
            if (domainEvent is INotification notification)
            {
                await publisher.Publish(notification, cancellationToken);
            }

            message.ProcessedAt = clock.GetCurrentInstant();
            message.Error = null;
        }
        catch (Exception ex)
        {
            message.RetryCount++;
            message.Error = ex.Message;
        }
    }
}
