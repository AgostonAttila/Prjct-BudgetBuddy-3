using BudgetBuddy.Shared.Messages.Contracts.Investments;

namespace BudgetBuddy.Service.Investments.Features.Investments.Services;

public class InvestmentDataService(InvestmentsDbContext context) : IInvestmentDataService
{
    public Task<List<InvestmentDataItem>> GetActiveInvestmentsAsync(
        string userId,
        LocalDate? startDate,
        LocalDate? endDate,
        InvestmentType? type,
        CancellationToken cancellationToken = default)
    {
        // Include fully unsold lots AND partially sold lots (SoldQuantity < Quantity).
        var query = context.Investments
            .Where(i => i.UserId == userId &&
                        (i.SoldDate == null || (i.SoldQuantity ?? 0) < i.Quantity));

        if (startDate.HasValue)
        {
            query = query.Where(i => i.PurchaseDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(i => i.PurchaseDate <= endDate.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(i => i.Type == type.Value);
        }

        return query
            .AsNoTracking()
            .Where(i => i.CurrencyCode != null)
            .Select(i => new InvestmentDataItem(
                i.Id,
                i.Symbol,
                i.Name,
                i.Type,
                i.Quantity - (i.SoldQuantity ?? 0),   // remaining quantity after FIFO sells
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
