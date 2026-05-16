namespace BudgetBuddy.Service.Analytics.ReadModels;

/// <summary>
/// Denormalized local copy of a Transaction, maintained via Kafka transaction events.
/// Backs the full ITransactionQueryService implementation in Analytics.
/// </summary>
public class TransactionReadModel
{
    public Guid TransactionId          { get; set; }
    public string UserId               { get; set; } = string.Empty;
    public Guid AccountId              { get; set; }
    public Guid? CategoryId            { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount              { get; set; }
    public string CurrencyCode         { get; set; } = string.Empty;
    public LocalDate TransactionDate   { get; set; }
    public DateTime SyncedAt           { get; set; }
}
