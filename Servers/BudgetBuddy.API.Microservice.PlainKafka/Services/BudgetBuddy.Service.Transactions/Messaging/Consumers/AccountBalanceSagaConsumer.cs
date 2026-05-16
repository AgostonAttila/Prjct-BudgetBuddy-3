using BudgetBuddy.Service.Transactions.Domain;
using BudgetBuddy.Service.Transactions.Persistence;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using BudgetBuddy.Shared.Messages.Topics;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace BudgetBuddy.Service.Transactions.Messaging.Consumers;

/// <summary>
/// Saga coordinator consumer — handles account-balance confirmation/rejection events
/// published by the Accounts service in response to a TransactionCreatedEvent.
///
/// On success  → logs confirmation; the transaction is already persisted and stands.
/// On failure  → publishes a compensation (reversal) transaction via the Outbox so that
///               the balance impact is undone in an eventually-consistent, at-least-once manner.
///
/// Idempotency: checks for an existing compensation before creating a new one, so that
/// duplicate AccountBalanceReservedEvent deliveries do not cause double reversals.
///
/// Choreography flow:
///   TransactionsCreated ──▶ AccountsService ──▶ AccountsBalanceReserved (success/fail)
///   AccountsBalanceReserved (fail) ──▶ here ──▶ reversal OutboxMessage
/// </summary>
public sealed class AccountBalanceSagaConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<AccountBalanceSagaConsumer> logger)
    : KafkaConsumerBase<AccountBalanceReservedEvent>(
        settings,
        TopicNames.AccountsBalanceReserved,
        ConsumerGroups.TransactionsSaga,
        scopeFactory,
        logger)
{
    protected override string? DlqTopic => TopicNames.AccountsDlq;

    protected override async Task ProcessAsync(
        AccountBalanceReservedEvent message,
        IServiceProvider services,
        CancellationToken ct)
    {
        var db    = services.GetRequiredService<TransactionsDbContext>();

        var original = await db.Transactions.FindAsync([message.TransactionId], ct);

        if (original is null)
        {
            Logger.LogWarning(
                "Saga: transaction {TransactionId} not found (correlation: {CorrelationId})",
                message.TransactionId, message.CorrelationId);
            return;
        }

        if (message.Success)
        {
            original.SagaStep = TransactionSagaStep.Confirmed;
            await db.SaveChangesAsync(ct);
            Logger.LogInformation(
                "Saga: transaction {TransactionId} confirmed — account balance reserved (correlation: {CorrelationId})",
                message.TransactionId, message.CorrelationId);
            return;
        }

        // Idempotency: if a compensation already exists for this original transaction,
        // skip to avoid double reversal on duplicate event delivery (at-least-once).
        var compensationNote = $"[SAGA COMPENSATION] Reversal of {original.Id}";
        var alreadyCompensated = await db.Transactions
            .AnyAsync(t => t.Note != null && t.Note.StartsWith(compensationNote), ct);

        if (alreadyCompensated)
        {
            original.SagaStep = TransactionSagaStep.Compensated;
            await db.SaveChangesAsync(ct);
            Logger.LogInformation(
                "Saga: compensation for {OriginalId} already exists — skipping duplicate event (correlation: {CorrelationId})",
                original.Id, message.CorrelationId);
            return;
        }

        original.SagaStep = TransactionSagaStep.Compensating;

        var reversal = new Transaction
        {
            Id              = Guid.NewGuid(),
            AccountId       = original.AccountId,
            Amount          = -original.Amount,          // opposite sign = reversal
            CurrencyCode    = original.CurrencyCode,
            TransactionType = original.TransactionType,
            PaymentType     = original.PaymentType,
            TransactionDate = original.TransactionDate,
            IsTransfer      = false,
            IsHidden        = true,                      // compensation reversal — hidden from user UI
            Note            = $"{compensationNote}: {message.Reason}",
            UserId          = original.UserId,
            SagaStep        = TransactionSagaStep.Confirmed, // reversal itself needs no saga
        };

        db.Transactions.Add(reversal);

        var collector = services.GetRequiredService<IDomainEventCollector>();
        collector.Collect(new TransactionReversedEvent
        {
            OriginalTransactionId = original.Id,
            ReversalTransactionId = reversal.Id,
            CorrelationId         = message.CorrelationId,
            Reason                = message.Reason ?? "Saga compensation",
        });

        await db.SaveChangesAsync(ct);

        original.SagaStep = TransactionSagaStep.Compensated;
        await db.SaveChangesAsync(ct);

        Logger.LogWarning(
            "Saga: compensated transaction {OriginalId} with reversal {ReversalId} — reason: {Reason}",
            original.Id, reversal.Id, message.Reason);
    }
}
