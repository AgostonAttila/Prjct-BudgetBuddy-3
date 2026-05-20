using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Investments;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace BudgetBuddy.Service.Analytics.Messaging.Handlers;

/// <summary>
/// Maintains the PriceSnapshotReadModel (USD only).
/// Replaces <c>MarketPriceUpdatedConsumer</c> (KafkaConsumerBase).
/// </summary>
[SupportedSchemaVersions("1")]
public class MarketPriceUpdatedHandler
{
    protected MarketPriceUpdatedHandler() { }

    public static async Task Handle(
        MarketPriceUpdatedEvent evt,
        AnalyticsDbContext db,
        IClock clock,
        ILogger<MarketPriceUpdatedHandler> logger,
        CancellationToken ct)
    {
        if (!string.Equals(evt.Currency, "USD", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Skipping non-USD price event for {Symbol} (currency={Currency})",
                evt.Symbol, evt.Currency);
            return;
        }

        var snapshotDate = LocalDate.FromDateTime(evt.OccurredAt.Date);
        var now          = clock.GetCurrentInstant();

        var existing = await db.PriceSnapshots
            .FirstOrDefaultAsync(p => p.Symbol == evt.Symbol && p.Date == snapshotDate, ct);

        if (existing is null)
        {
            db.PriceSnapshots.Add(new PriceSnapshotReadModel
            {
                Symbol   = evt.Symbol,
                Date     = snapshotDate,
                PriceUsd = evt.Price,
                SyncedAt = now,
            });
        }
        else
        {
            existing.PriceUsd = evt.Price;
            existing.SyncedAt = now;
        }

        // Wolverine's EF Core integration commits the transaction automatically after the handler returns.
        // Do NOT call SaveChangesAsync() here — it would break the outbox transaction boundary.
    }
}
