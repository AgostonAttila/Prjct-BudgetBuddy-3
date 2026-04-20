using BudgetBuddy.Shared.Contracts.Events.Transactions;
using BudgetBuddy.Shared.Infrastructure;
using MediatR;

namespace BudgetBuddy.Module.Analytics.Features.Transactions;

/// <summary>
/// Handles <see cref="TransactionDeletedEvent"/> published by the Transactions module via the Outbox processor.
///
/// Currently: invalidates the HybridCache for the affected user so that deleted transactions
/// are no longer reflected in dashboard totals or reports.
///
/// Future extensions to consider:
/// - Remove or reverse the deleted transaction's contribution from pre-aggregated read models.
/// - Recalculate affected monthly/category summaries if stored as materialized data.
/// </summary>
public class TransactionDeletedHandler(
    IUserCacheInvalidator userCacheInvalidator,
    ILogger<TransactionDeletedHandler> logger)
    : INotificationHandler<TransactionDeletedEvent>
{
    public async Task Handle(TransactionDeletedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Analytics: invalidating dashboard cache for user {UserId} after TransactionDeletedEvent {TransactionId}",
            notification.UserId, notification.TransactionId);

        await userCacheInvalidator.InvalidateAsync(notification.UserId, cancellationToken);
    }
}
