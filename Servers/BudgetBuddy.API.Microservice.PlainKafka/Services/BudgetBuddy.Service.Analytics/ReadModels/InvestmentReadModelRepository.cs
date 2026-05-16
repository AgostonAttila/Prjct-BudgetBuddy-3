using BudgetBuddy.Service.Analytics.Persistence;

namespace BudgetBuddy.Service.Analytics.ReadModels;

public class InvestmentReadModelRepository(AnalyticsDbContext context) : IInvestmentReadModelRepository
{
    public async Task UpsertAsync(InvestmentReadModel model, CancellationToken ct = default)
    {
        var existing = await context.Investments.FindAsync([model.InvestmentId], ct);
        if (existing is null)
        {
            context.Investments.Add(model);
        }
        else
        {
            existing.UserId        = model.UserId;
            existing.Symbol        = model.Symbol;
            existing.Name          = model.Name;
            existing.Type          = model.Type;
            existing.Quantity      = model.Quantity;
            existing.PurchasePrice = model.PurchasePrice;
            existing.CurrencyCode  = model.CurrencyCode;
            existing.PurchaseDate  = model.PurchaseDate;
            existing.SyncedAt      = model.SyncedAt;
        }
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid investmentId, CancellationToken ct = default)
    {
        var existing = await context.Investments.FindAsync([investmentId], ct);
        if (existing is not null)
        {
            context.Investments.Remove(existing);
            await context.SaveChangesAsync(ct);
        }
    }
}
