using System.Text.Json;
using BudgetBuddy.Service.Budgets.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.Budgets.Messaging.Consumers;

/// <summary>
/// Consumes transaction events to maintain the CategorySpendingAggregate read model.
/// Processes expense transactions only; income and transfers are ignored.
/// </summary>
public sealed class TransactionSpendingConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<TransactionSpendingConsumer> logger)
    : KafkaMultiTopicConsumerBase(
        settings,
        [TopicNames.TransactionsCreated, TopicNames.TransactionsUpdated, TopicNames.TransactionsDeleted],
        ConsumerGroups.BudgetsTransactions,
        scopeFactory,
        logger)
{
    protected override string? DlqTopic => TopicNames.TransactionsDlq;

    protected override async Task ProcessAsync(
        string topic, string? payload, string messageKey, long offset,
        IServiceProvider services, CancellationToken ct)
    {
        var repo = services.GetRequiredService<ICategorySpendingRepository>();

        switch (topic)
        {
            case TopicNames.TransactionsCreated:
            {
                var evt = JsonSerializer.Deserialize<TransactionCreatedEvent>(payload!)!;
                if (evt.TransactionType == TransactionType.Expense && evt.CategoryId is not null)
                    {
                        await repo.AddToExpenseAsync(
                        evt.UserId, evt.CategoryId.Value,
                        evt.TransactionDate.Year, evt.TransactionDate.Month,
                        evt.CurrencyCode, evt.Amount, ct);
                    }

                    break;
            }
            case TopicNames.TransactionsUpdated:
            {
                var evt = JsonSerializer.Deserialize<TransactionUpdatedEvent>(payload!)!;
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
                break;
            }
            case TopicNames.TransactionsDeleted:
            {
                var evt = JsonSerializer.Deserialize<TransactionDeletedEvent>(payload!)!;
                if (evt.TransactionType == TransactionType.Expense && evt.CategoryId is not null)
                    {
                        await repo.AddToExpenseAsync(
                        evt.UserId, evt.CategoryId.Value,
                        evt.TransactionDate.Year, evt.TransactionDate.Month,
                        evt.CurrencyCode, -evt.Amount, ct);
                    }

                    break;
            }
        }
    }
}
