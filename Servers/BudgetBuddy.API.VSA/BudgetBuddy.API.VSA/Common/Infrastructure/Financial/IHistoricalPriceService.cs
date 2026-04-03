namespace BudgetBuddy.API.VSA.Common.Infrastructure.Financial;

/// <summary>
/// Orchestrates historical price lookups, routing to the correct provider by InvestmentType.
/// </summary>
public interface IHistoricalPriceService
{
    Task<Dictionary<LocalDate, decimal>> GetDailyClosePricesAsync(
        string symbol,
        InvestmentType type,
        LocalDate from,
        CancellationToken cancellationToken = default);
}
