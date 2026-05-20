using BudgetBuddy.Service.Transactions.Domain;
using BudgetBuddy.Service.Transactions.Persistence;
using BudgetBuddy.Service.Transactions.Sagas;
using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Quartz;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Transactions.Features.Transactions.Jobs;

/// <summary>
/// Resolves transactions stuck in <see cref="TransactionSagaStep.Started"/> because the
/// Accounts service never published an <c>AccountBalanceReservedEvent</c> (crash, network
/// partition, lost Kafka message).
///
/// Every 30 seconds the job scans for transactions where SagaStep = Started AND
/// CreatedAt is older than <see cref="TimeoutSeconds"/>. For each stuck transaction it
/// creates the same compensation reversal that <c>AccountBalanceSagaConsumer</c> would
/// have created, ensuring the balance impact is always undone in a finite time window.
///
/// Idempotency: the existing compensation-note prefix guard prevents double-reversal if the
/// Accounts service response and the timeout job race to compensate the same transaction.
/// </summary>
[DisallowConcurrentExecution]
public class SagaTimeoutJob(
    IServiceScopeFactory scopeFactory,
    ILogger<SagaTimeoutJob> logger) : ScheduledJobBase(logger)
{
    private const int TimeoutSeconds = 30;

    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db    = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var cutoff = clock.GetCurrentInstant() - Duration.FromSeconds(TimeoutSeconds);

        var stuck = await db.Transactions
            .Where(t => t.SagaStep == TransactionSagaStep.Started && t.CreatedAt < cutoff)
            .ToListAsync(ct);

        if (stuck.Count == 0)
        {
            return;
        }

        Logger.LogWarning("SagaTimeoutJob: found {Count} stuck transaction(s) older than {Timeout}s",
            stuck.Count, TimeoutSeconds);

        var outbox = scope.ServiceProvider.GetRequiredService<IDbContextOutbox>();
        outbox.Enroll(db);

        foreach (var tx in stuck)
        {
            await CompensateAsync(tx, db, outbox, ct);
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
    }

    private async Task CompensateAsync(
        Transaction tx,
        TransactionsDbContext db,
        IDbContextOutbox outbox,
        CancellationToken ct)
    {
        var compensationNote = $"[SAGA COMPENSATION] Reversal of {tx.Id}";

        var alreadyCompensated = await db.Transactions
            .AnyAsync(t => t.Note != null && t.Note.StartsWith(compensationNote), ct);

        if (alreadyCompensated)
        {
            tx.SagaStep = TransactionSagaStep.Compensated;
            Logger.LogInformation(
                "SagaTimeoutJob: compensation already exists for {TransactionId} — marking Compensated",
                tx.Id);
            return;
        }

        tx.SagaStep = TransactionSagaStep.Failed;

        var reversal = new Transaction
        {
            Id              = Guid.NewGuid(),
            AccountId       = tx.AccountId,
            Amount          = -tx.Amount,
            CurrencyCode    = tx.CurrencyCode,
            TransactionType = tx.TransactionType,
            PaymentType     = tx.PaymentType,
            TransactionDate = tx.TransactionDate,
            IsTransfer      = false,
            IsHidden        = true,                      // compensation reversal — hidden from user UI
            Note            = $"{compensationNote}: saga timeout after {TimeoutSeconds}s",
            UserId          = tx.UserId,
            SagaStep        = TransactionSagaStep.Confirmed,
        };

        db.Transactions.Add(reversal);

        await outbox.SendAsync(new TransactionReversedEvent
        {
            OriginalTransactionId = tx.Id,
            ReversalTransactionId = reversal.Id,
            CorrelationId         = Guid.NewGuid(),
            Reason                = $"Saga timeout — no AccountBalanceReservedEvent received within {TimeoutSeconds}s",
        });

        Logger.LogWarning(
            "SagaTimeoutJob: compensated stuck transaction {OriginalId} with reversal {ReversalId}",
            tx.Id, reversal.Id);
    }
}
