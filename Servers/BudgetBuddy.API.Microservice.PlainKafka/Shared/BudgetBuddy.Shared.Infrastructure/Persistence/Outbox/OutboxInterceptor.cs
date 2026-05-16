using System.Text.Json;
using BudgetBuddy.Shared.Messages.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

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
    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions { WriteIndented = false }
            .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

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
        if (context is null)
        {
            return;
        }

        // Cache the model check — FindEntityType is O(n) reflection, model never changes at runtime
        _hasOutboxSet ??= context.Model.FindEntityType(typeof(OutboxMessage)) is not null;
        if (!_hasOutboxSet.Value)
        {
            return;
        }

        var events = collector.GetAndClear();
        if (events.Count == 0)
        {
            return;
        }

        var now = clock.GetCurrentInstant();

        foreach (var integrationEvent in events)
        {
            var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions);

            var message = new OutboxMessage
            {
                Id = integrationEvent.MessageId,
                // Store only the short type name (namespace-independent) so that
                // outbox rows survive assembly/namespace refactors.
                EventType = integrationEvent.GetType().Name,
                Payload = payload,
                CreatedAt = now
            };

            context.Set<OutboxMessage>().Add(message);
        }
    }
}
