namespace BudgetBuddy.Service.Analytics.ReadModels;

/// <summary>
/// Local copy of an Investment, maintained via Kafka investment events.
/// Backs IInvestmentDataService.GetActiveInvestmentsAsync in Analytics.
/// Historical FX rates are fetched on-demand via IFxHistoricalProvider.
/// </summary>
public class InvestmentReadModel
{
    public Guid InvestmentId    { get; set; }
    public string UserId        { get; set; } = string.Empty;
    public string Symbol        { get; set; } = string.Empty;
    public string Name          { get; set; } = string.Empty;
    public InvestmentType Type  { get; set; }
    public decimal Quantity     { get; set; }
    public decimal PurchasePrice { get; set; }
    public string CurrencyCode  { get; set; } = string.Empty;
    public LocalDate PurchaseDate { get; set; }
    public DateTime SyncedAt    { get; set; }
}
