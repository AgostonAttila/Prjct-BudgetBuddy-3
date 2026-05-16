namespace BudgetBuddy.Service.Transactions.ReadModels;

/// <summary>
/// Local read model of a Category, maintained via the Kafka CategoryChanged topic.
/// Allows the Transactions service to resolve category names without HTTP calls.
/// </summary>
public class CategorySnapshot
{
    public Guid CategoryId { get; set; }
    public string UserId   { get; set; } = string.Empty;
    public string Name     { get; set; } = string.Empty;
    public string? Icon    { get; set; }
    public DateTime SyncedAt { get; set; }
}
