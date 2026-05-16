namespace BudgetBuddy.Service.Budgets.ReadModels;

public class CategorySnapshot
{
    public Guid CategoryId { get; set; }
    public string UserId   { get; set; } = string.Empty;
    public string Name     { get; set; } = string.Empty;
    public string? Icon    { get; set; }
    public DateTime SyncedAt { get; set; }
}
