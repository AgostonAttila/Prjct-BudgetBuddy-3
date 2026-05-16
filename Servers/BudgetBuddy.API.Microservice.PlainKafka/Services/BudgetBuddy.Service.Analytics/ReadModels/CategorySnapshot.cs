namespace BudgetBuddy.Service.Analytics.ReadModels;

/// <summary>
/// Local read model of a Category, maintained via the referencedata.category-changed topic.
/// Allows Analytics to resolve category names without cross-service calls.
/// </summary>
public class CategorySnapshot
{
    public Guid CategoryId { get; set; }
    public string UserId   { get; set; } = string.Empty;
    public string Name     { get; set; } = string.Empty;
    public string? Icon    { get; set; }
    public DateTime SyncedAt { get; set; }
}
