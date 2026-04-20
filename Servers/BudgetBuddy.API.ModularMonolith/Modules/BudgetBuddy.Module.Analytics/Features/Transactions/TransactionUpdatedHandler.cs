using BudgetBuddy.Shared.Contracts.Events.Transactions;
using BudgetBuddy.Shared.Infrastructure;
using MediatR;

namespace BudgetBuddy.Module.Analytics.Features.Transactions;

/// <summary>
/// Handles <see cref="TransactionUpdatedEvent"/> published by the Transactions module via the Outbox processor.
///
/// Currently: invalidates the HybridCache for the affected user so that the next dashboard/report
/// request reflects the updated transaction data.
///
/// Future extensions to consider:
/// - Patch pre-aggregated read models to reflect the changed amount, category, or date.
/// - Re-evaluate budget alerts if the transaction's category or amount changed significantly.
/// - Recalculate incremental summaries (monthly totals, category spending) without a full recompute.
/// </summary>
public class TransactionUpdatedHandler(
    IUserCacheInvalidator userCacheInvalidator,
    ILogger<TransactionUpdatedHandler> logger)
    : INotificationHandler<TransactionUpdatedEvent>
{
    public async Task Handle(TransactionUpdatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Analytics: invalidating dashboard cache for user {UserId} after TransactionUpdatedEvent {TransactionId}",
            notification.UserId, notification.TransactionId);

        await userCacheInvalidator.InvalidateAsync(notification.UserId, cancellationToken);
    }
}
