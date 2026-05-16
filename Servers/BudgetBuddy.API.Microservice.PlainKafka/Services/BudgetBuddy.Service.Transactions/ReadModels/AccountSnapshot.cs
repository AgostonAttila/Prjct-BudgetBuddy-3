namespace BudgetBuddy.Service.Transactions.ReadModels;

/// <summary>
/// Local read model of an Account, maintained via the Kafka compacted changelog topic.
/// Allows the Transactions service to validate account ownership without HTTP calls.
/// </summary>
public class AccountSnapshot
{
    public Guid AccountId { get; set; }
    public string UserId  { get; set; } = string.Empty;
    public string Name    { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long Version { get; set; }   // Kafka offset — out-of-order protection
    public DateTime SyncedAt { get; set; }
}
