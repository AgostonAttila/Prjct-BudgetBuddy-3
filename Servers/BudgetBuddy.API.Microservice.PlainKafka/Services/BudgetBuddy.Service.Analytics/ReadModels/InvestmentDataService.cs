using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Shared.Messages.Contracts.Financial;
using BudgetBuddy.Shared.Messages.Contracts.Investments;

namespace BudgetBuddy.Service.Analytics.ReadModels;

public class InvestmentDataService(
    AnalyticsDbContext context,
    IFxHistoricalProvider fxProvider) : IInvestmentDataService
{
    public Task<List<InvestmentDataItem>> GetActiveInvestmentsAsync(
        string userId,
        LocalDate? startDate,
        LocalDate? endDate,
        InvestmentType? type,
        CancellationToken cancellationToken = default)
        => context.Investments
            .Where(i => i.UserId == userId
                && (startDate == null || i.PurchaseDate >= startDate.Value)
                && (endDate == null || i.PurchaseDate <= endDate.Value)
                && (type == null || i.Type == type.Value))
            .Select(i => new InvestmentDataItem(
                i.InvestmentId, i.Symbol, i.Name, i.Type,
                i.Quantity, i.PurchasePrice, i.PurchaseDate, i.CurrencyCode))
            .ToListAsync(cancellationToken);

    public async Task<List<ExchangeRateSnapshotItem>> GetExchangeRateSnapshotsAsync(
        LocalDate from, LocalDate to, CancellationToken cancellationToken = default)
    {
        var rates = await fxProvider.GetDailyRatesAsync(from, to, "USD", cancellationToken);
        return rates
            .SelectMany(kvp => kvp.Value.Select(r => new ExchangeRateSnapshotItem(r.Key, kvp.Key, r.Value)))
            .ToList();
    }
}
