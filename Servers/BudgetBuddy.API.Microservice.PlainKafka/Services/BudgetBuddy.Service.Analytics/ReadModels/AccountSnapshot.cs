namespace BudgetBuddy.Service.Analytics.ReadModels;

/// <summary>
/// Local read model of an Account, maintained via the compacted accounts.changelog topic.
/// Stores the initial balance so AccountBalanceService can calculate running balances
/// without cross-service HTTP calls.
/// </summary>
public class AccountSnapshot
{
    public Guid AccountId  { get; set; }
    public string UserId   { get; set; } = string.Empty;
    public string Name     { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool IsActive   { get; set; }
    public decimal Balance { get; set; }  // Initial/opening balance from AccountSnapshotMessage
    public long Version    { get; set; }  // Kafka offset — out-of-order protection
    public DateTime SyncedAt { get; set; }
}
