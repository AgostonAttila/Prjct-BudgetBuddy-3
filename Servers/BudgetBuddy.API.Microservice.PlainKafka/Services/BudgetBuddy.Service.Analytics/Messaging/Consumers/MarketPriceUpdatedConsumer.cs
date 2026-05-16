using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Investments;
using BudgetBuddy.Shared.Messages.Topics;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace BudgetBuddy.Service.Analytics.Messaging.Consumers;

/// <summary>
/// Consumes MarketPriceUpdatedEvent to maintain a local PriceSnapshotReadModel.
/// Prices are stored in USD (the publisher always emits USD via DailyPriceSnapshotJob).
/// </summary>
public sealed class MarketPriceUpdatedConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<MarketPriceUpdatedConsumer> logger,
    IClock clock)
    : KafkaConsumerBase<MarketPriceUpdatedEvent>(
        settings,
        TopicNames.MarketPriceUpdated,
        ConsumerGroups.AnalyticsMarketPrice,
        scopeFactory,
        logger)
{
    protected override string? DlqTopic => TopicNames.InvestmentsDlq;

    protected override async Task ProcessAsync(
        MarketPriceUpdatedEvent evt, IServiceProvider services, CancellationToken ct)
    {
        // Publisher (DailyPriceSnapshotJob) always emits USD prices.
        // If a future publisher emits a different currency, skip it to keep the
        // read model currency-homogeneous (USD only).
        if (!string.Equals(evt.Currency, "USD", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning(
                "Skipping non-USD price event for {Symbol} (currency={Currency})",
                evt.Symbol, evt.Currency);
            return;
        }

        var snapshotDate = LocalDate.FromDateTime(evt.OccurredAt.Date);
        var now          = clock.GetCurrentInstant();

        var db = services.GetRequiredService<AnalyticsDbContext>();

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

        await db.SaveChangesAsync(ct);
    }
}
