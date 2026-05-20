namespace BudgetBuddy.Service.Accounts.ReadModels;

/// <summary>
/// Running totals per account, built from transaction events.
/// Used to calculate account balances without cross-service HTTP calls.
/// Note: upToDate filtering is not supported in Fázis 2 (running totals only).
/// </summary>
public class AccountTransactionTotals
{
    public Guid    AccountId              { get; set; }
    public decimal TotalIncome            { get; set; }
    public decimal TotalExpense           { get; set; }
    public int     TransactionCount       { get; set; }
    public DateTime SyncedAt             { get; set; }
    /// <summary>
    /// MessageId of the last successfully applied Kafka event.
    /// Used for idempotency: if the same MessageId arrives again (at-least-once retry),
    /// the delta is skipped to prevent double-counting.
    /// </summary>
    public Guid? LastProcessedMessageId  { get; set; }
}
