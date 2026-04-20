using BudgetBuddy.Shared.Contracts.Investments;

namespace BudgetBuddy.Module.Investments.Features.Investments.Services;

public class InvestmentDataService(InvestmentsDbContext context) : IInvestmentDataService
{
    public Task<List<InvestmentDataItem>> GetActiveInvestmentsAsync(
        string userId,
        LocalDate? startDate,
        LocalDate? endDate,
        InvestmentType? type,
        CancellationToken cancellationToken = default)
    {
        var query = context.Investments
            .Where(i => i.UserId == userId && i.SoldDate == null);

        if (startDate.HasValue) query = query.Where(i => i.PurchaseDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(i => i.PurchaseDate <= endDate.Value);
        if (type.HasValue) query = query.Where(i => i.Type == type.Value);

        return query
            .AsNoTracking()
            .Where(i => i.CurrencyCode != null)
            .Select(i => new InvestmentDataItem(
                i.Id,
                i.Symbol,
                i.Name,
                i.Type,
                i.Quantity,
                i.PurchasePrice,
                i.PurchaseDate,
                i.CurrencyCode.ToUpperInvariant()))
            .ToListAsync(cancellationToken);
    }

    public Task<List<ExchangeRateSnapshotItem>> GetExchangeRateSnapshotsAsync(
        LocalDate from,
        LocalDate to,
        CancellationToken cancellationToken = default) =>
        context.ExchangeRateSnapshots
            .AsNoTracking()
            .Where(e => e.Date >= from && e.Date <= to)
            .OrderBy(e => e.Date)
            .Select(e => new ExchangeRateSnapshotItem(
                e.Currency.ToUpperInvariant(),
                e.Date,
                e.RateToUsd))
            .ToListAsync(cancellationToken);
}
