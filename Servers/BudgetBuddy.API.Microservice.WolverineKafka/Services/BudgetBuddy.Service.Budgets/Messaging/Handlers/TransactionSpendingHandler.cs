using BudgetBuddy.Service.Budgets.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Transactions;

namespace BudgetBuddy.Service.Budgets.Messaging.Handlers;

/// <summary>
/// Maintains the CategorySpendingAggregate read model for budget utilisation.
/// Replaces <c>TransactionSpendingConsumer</c> (KafkaMultiTopicConsumerBase).
/// Processes expense transactions only — income and transfers are ignored.
/// </summary>
[SupportedSchemaVersions("1")]
public static class TransactionSpendingHandler
{
    public static async Task Handle(
        TransactionCreatedEvent evt,
        ICategorySpendingRepository repo,
        CancellationToken ct)
    {
        if (evt.TransactionType == TransactionType.Expense && evt.CategoryId is not null)
        {
            await repo.AddToExpenseAsync(
                evt.UserId, evt.CategoryId.Value,
                evt.TransactionDate.Year, evt.TransactionDate.Month,
                evt.CurrencyCode, evt.Amount, ct);
        }
    }

    public static async Task Handle(
        TransactionUpdatedEvent evt,
        ICategorySpendingRepository repo,
        CancellationToken ct)
    {
        if (evt.TransactionType == TransactionType.Expense && evt.CategoryId is not null)
        {
            var delta = evt.Amount - evt.OldAmount;
            if (delta != 0)
            {
                await repo.AddToExpenseAsync(
                    evt.UserId, evt.CategoryId.Value,
                    evt.TransactionDate.Year, evt.TransactionDate.Month,
                    evt.CurrencyCode, delta, ct);
            }
        }
    }

    public static async Task Handle(
        TransactionDeletedEvent evt,
        ICategorySpendingRepository repo,
        CancellationToken ct)
    {
        if (evt.TransactionType == TransactionType.Expense && evt.CategoryId is not null)
        {
            await repo.AddToExpenseAsync(
                evt.UserId, evt.CategoryId.Value,
                evt.TransactionDate.Year, evt.TransactionDate.Month,
                evt.CurrencyCode, -evt.Amount, ct);
        }
    }
}
