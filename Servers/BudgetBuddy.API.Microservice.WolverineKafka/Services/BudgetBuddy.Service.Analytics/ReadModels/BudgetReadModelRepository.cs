using BudgetBuddy.Service.Analytics.Persistence;

namespace BudgetBuddy.Service.Analytics.ReadModels;

public class BudgetReadModelRepository(AnalyticsDbContext context) : IBudgetReadModelRepository
{
    public async Task UpsertAsync(BudgetReadModel model, CancellationToken ct = default)
    {
        var existing = await context.Budgets.FindAsync([model.BudgetId], ct);
        if (existing is null)
        {
            context.Budgets.Add(model);
        }
        else
        {
            existing.UserId       = model.UserId;
            existing.CategoryId   = model.CategoryId;
            existing.Amount       = model.Amount;
            existing.CurrencyCode = model.CurrencyCode;
            existing.Year         = model.Year;
            existing.Month        = model.Month;
            existing.SyncedAt     = model.SyncedAt;
        }
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid budgetId, CancellationToken ct = default)
    {
        var existing = await context.Budgets.FindAsync([budgetId], ct);
        if (existing is not null)
        {
            context.Budgets.Remove(existing);
            await context.SaveChangesAsync(ct);
        }
    }
}
