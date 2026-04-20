using BudgetBuddy.Shared.Contracts.Events.Transactions;
using BudgetBuddy.Shared.Infrastructure;
using MediatR;

namespace BudgetBuddy.Module.Analytics.Features.Transactions;

/// <summary>
/// Handles <see cref="TransactionCreatedEvent"/> published by the Transactions module via the Outbox processor.
///
/// Currently: invalidates the HybridCache for the affected user so that the next dashboard/report
/// request fetches fresh data. Note that the Transactions handler already calls cache invalidation
/// synchronously at the end of the request — this handler acts as a guaranteed safety net via the Outbox.
///
/// Future extensions to consider:
/// - Update pre-aggregated read models or materialized views if Analytics gets its own read store.
/// - Trigger real-time budget threshold checks (e.g. notify if a spending category is close to its limit).
/// - Recalculate and persist monthly/yearly summaries incrementally instead of on-demand.
/// - Push a server-sent event or WebSocket notification to the frontend for live dashboard updates.
/// </summary>
public class TransactionCreatedHandler(
    IUserCacheInvalidator userCacheInvalidator,
    ILogger<TransactionCreatedHandler> logger)
    : INotificationHandler<TransactionCreatedEvent>
{
    public async Task Handle(TransactionCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Analytics: invalidating dashboard cache for user {UserId} after TransactionCreatedEvent {TransactionId}",
            notification.UserId, notification.TransactionId);

        await userCacheInvalidator.InvalidateAsync(notification.UserId, cancellationToken);
    }
}
