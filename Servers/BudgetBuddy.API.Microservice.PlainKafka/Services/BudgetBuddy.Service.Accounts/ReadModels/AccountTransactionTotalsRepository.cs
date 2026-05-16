using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Service.Accounts.ReadModels;

public class AccountTransactionTotalsRepository(AccountsDbContext context) : IAccountTransactionTotalsRepository
{
    public Task<AccountTransactionTotals?> FindAsync(Guid accountId, CancellationToken ct)
        => context.AccountTransactionTotals
            .FirstOrDefaultAsync(t => t.AccountId == accountId, ct);

    public Task<List<AccountTransactionTotals>> FindByIdsAsync(IEnumerable<Guid> accountIds, CancellationToken ct)
    {
        var ids = accountIds.ToList();
        return context.AccountTransactionTotals
            .Where(t => ids.Contains(t.AccountId))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Applies an income/expense/count delta atomically via a single PostgreSQL upsert.
    ///
    /// Replaces the previous read-modify-write pattern that had a lost-update race condition
    /// when two Kafka consumer instances processed events for the same account concurrently
    /// (possible when events land on different partitions).
    ///
    /// The ON CONFLICT DO UPDATE generates a single server-side arithmetic UPDATE, so no
    /// application-level read is needed and no optimistic concurrency token is required.
    /// Idempotency is enforced by the WHERE clause: if last_processed_message_id already
    /// equals messageId (at-least-once duplicate delivery), the DO UPDATE is skipped.
    /// </summary>
    public async Task ApplyDeltaAsync(
        Guid accountId, decimal incomeDelta, decimal expenseDelta, int countDelta, Guid messageId, CancellationToken ct)
    {
        await context.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO accounts.account_transaction_totals
                 (account_id, total_income, total_expense, transaction_count, synced_at, last_processed_message_id)
             VALUES
                 ({accountId},
                  GREATEST(0, {incomeDelta}::numeric),
                  GREATEST(0, {expenseDelta}::numeric),
                  GREATEST(0, {countDelta}::integer),
                  NOW(),
                  {messageId})
             ON CONFLICT (account_id) DO UPDATE
             SET total_income              = GREATEST(0, accounts.account_transaction_totals.total_income  + {incomeDelta}::numeric),
                 total_expense             = GREATEST(0, accounts.account_transaction_totals.total_expense + {expenseDelta}::numeric),
                 transaction_count         = GREATEST(0, accounts.account_transaction_totals.transaction_count + {countDelta}::integer),
                 synced_at                 = NOW(),
                 last_processed_message_id = {messageId}
             WHERE accounts.account_transaction_totals.last_processed_message_id IS DISTINCT FROM {messageId}
             """,
            ct);
    }
}
