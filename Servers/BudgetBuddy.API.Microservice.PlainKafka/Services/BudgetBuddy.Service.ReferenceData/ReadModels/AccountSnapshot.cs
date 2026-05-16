namespace BudgetBuddy.Service.ReferenceData.ReadModels;

public class AccountSnapshot
{
    public Guid AccountId  { get; set; }
    public string UserId   { get; set; } = string.Empty;
    public string Name     { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool IsActive   { get; set; }
    public long Version    { get; set; }
    public DateTime SyncedAt { get; set; }
}
