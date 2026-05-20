using BudgetBuddy.Service.Transactions.Domain;
using BudgetBuddy.Service.Transactions.Persistence;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace BudgetBuddy.Service.Transactions.Messaging.Handlers;

/// <summary>
/// Wolverine handler replacing <c>AccountBalanceSagaConsumer</c>.
///
/// Wolverine (Eager EF Core mode) automatically wraps the handler in a transaction
/// and calls SaveChangesAsync on completion — the handler must NOT call SaveChangesAsync
/// or Enroll/SaveChangesAndFlushMessagesAsync manually.
///
/// Publishing inside a Wolverine handler is done via <see cref="IMessageContext"/> which
/// participates in the same ambient transaction (transactional outbox).
/// </summary>
[SupportedSchemaVersions("1")]
public class AccountBalanceSagaHandler
{
    protected AccountBalanceSagaHandler() { }

    public static async Task Handle(
        AccountBalanceReservedEvent message,
        TransactionsDbContext db,
        IMessageContext bus,
        ILogger<AccountBalanceSagaHandler> logger,
        CancellationToken ct)
    {
        var original = await db.Transactions.FindAsync([message.TransactionId], ct);

        if (original is null)
        {
            // ERROR: compensation event arrived for a non-existent transaction.
            // This is a saga invariant violation — possible causes: message reordering, data loss,
            // or the original create handler never committed. Must be investigated manually.
            logger.LogError(
                "Saga invariant violation: transaction {TransactionId} not found for compensation — " +
                "possible message reordering or data loss (correlation: {CorrelationId})",
                message.TransactionId, message.CorrelationId);
            throw new InvalidOperationException(
                $"Saga invariant violation: transaction {message.TransactionId} not found.");
        }

        if (message.Success)
        {
            original.SagaStep = TransactionSagaStep.Confirmed;
            logger.LogInformation(
                "Saga: transaction {TransactionId} confirmed — account balance reserved (correlation: {CorrelationId})",
                message.TransactionId, message.CorrelationId);
            return;
        }

        // Idempotency: the SagaStep state machine is the authoritative deduplication key.
        // Because Wolverine wraps the entire handler in a single DB transaction, SagaStep
        // can only reach Compensating/Compensated if the full compensation committed —
        // so a retry that finds either of these states safely skips re-processing.
        if (original.SagaStep is TransactionSagaStep.Compensating or TransactionSagaStep.Compensated)
        {
            logger.LogInformation(
                "Saga: compensation for {OriginalId} already processed (step={Step}) — " +
                "skipping duplicate event (correlation: {CorrelationId})",
                original.Id, original.SagaStep, message.CorrelationId);
            return;
        }

        original.SagaStep = TransactionSagaStep.Compensating;

        var compensationNote = $"[SAGA COMPENSATION] Reversal of {original.Id}";
        var reversal = new Transaction
        {
            Id              = Guid.NewGuid(),
            AccountId       = original.AccountId,
            Amount          = -original.Amount,
            CurrencyCode    = original.CurrencyCode,
            TransactionType = original.TransactionType,
            PaymentType     = original.PaymentType,
            TransactionDate = original.TransactionDate,
            IsTransfer      = false,
            IsHidden        = true,
            Note            = $"{compensationNote}: {message.Reason}",
            UserId          = original.UserId,
            SagaStep        = TransactionSagaStep.Confirmed,
        };

        db.Transactions.Add(reversal);

        // IMessageContext publishes into the same Wolverine outbox transaction
        await bus.PublishAsync(new TransactionReversedEvent
        {
            OriginalTransactionId = original.Id,
            ReversalTransactionId = reversal.Id,
            CorrelationId         = message.CorrelationId,
            Reason                = message.Reason ?? "Saga compensation",
        });

        original.SagaStep = TransactionSagaStep.Compensated;

        logger.LogWarning(
            "Saga: compensated transaction {OriginalId} with reversal {ReversalId} — reason: {Reason}",
            original.Id, reversal.Id, message.Reason);
    }
}
