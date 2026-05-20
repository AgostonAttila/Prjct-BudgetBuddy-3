namespace BudgetBuddy.Service.Analytics.ReadModels;

/// <summary>
/// Local copy of a Budget, maintained via Kafka budget events.
/// Backs IBudgetQueryService in Analytics.
/// </summary>
public class BudgetReadModel
{
    public Guid BudgetId       { get; set; }
    public string UserId       { get; set; } = string.Empty;
    public Guid CategoryId     { get; set; }
    public decimal Amount      { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int Year            { get; set; }
    public int Month           { get; set; }
    public DateTime SyncedAt   { get; set; }
}
