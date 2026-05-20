namespace BudgetBuddy.Service.Budgets.ReadModels;

/// <summary>
/// Monthly spending aggregate per category, built from transaction events.
/// Key: (UserId, CategoryId, Year, Month, CurrencyCode)
/// </summary>
public class CategorySpendingAggregate
{
    public Guid Id            { get; set; }
    public string UserId      { get; set; } = string.Empty;
    public Guid CategoryId    { get; set; }
    public int Year           { get; set; }
    public int Month          { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal TotalExpense { get; set; }
}
