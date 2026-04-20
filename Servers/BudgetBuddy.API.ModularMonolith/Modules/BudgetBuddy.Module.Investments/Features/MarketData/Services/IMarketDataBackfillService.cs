namespace BudgetBuddy.Module.Investments.Features.MarketData.Services;

public interface IMarketDataBackfillService
{
    /// <summary>
    /// Scans investments and currencies for gaps in historical snapshot data and fills them.
    /// Safe to call repeatedly — skips symbols/dates already present.
    /// </summary>
    Task BackfillAllMissingAsync(CancellationToken cancellationToken = default);
}
