using System.Text.Json;
using BudgetBuddy.Shared.Kernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NodaTime;

namespace BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;

/// <summary>
/// Scoped SaveChangesInterceptor. Before each SaveChanges, drains the
/// IDomainEventCollector and writes OutboxMessage rows into the same transaction.
/// Only active when the DbContext exposes a DbSet&lt;OutboxMessage&gt;.
/// </summary>
public sealed class OutboxInterceptor(
    IDomainEventCollector collector,
    IClock clock) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    // Cached per Scoped instance — model never changes after EF builds it
    private bool? _hasOutboxSet;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        PersistOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        PersistOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void PersistOutboxMessages(DbContext? context)
    {
        if (context is null) return;

        // Cache the model check — FindEntityType is O(n) reflection, model never changes at runtime
        _hasOutboxSet ??= context.Model.FindEntityType(typeof(OutboxMessage)) is not null;
        if (!_hasOutboxSet.Value) return;

        var events = collector.GetAndClear();
        if (events.Count == 0) return;

        var now = clock.GetCurrentInstant();

        foreach (var domainEvent in events)
        {
            var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);

            var message = new OutboxMessage
            {
                Id = domainEvent.EventId,
                EventType = domainEvent.GetType().AssemblyQualifiedName!,
                Payload = payload,
                CreatedAt = now
            };

            context.Set<OutboxMessage>().Add(message);
        }
    }
}
