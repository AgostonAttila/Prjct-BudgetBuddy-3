using BudgetBuddy.Service.Analytics.Persistence;

namespace BudgetBuddy.Service.Analytics.ReadModels;

public class TransactionReadModelRepository(AnalyticsDbContext context) : ITransactionReadModelRepository
{
    public async Task UpsertAsync(TransactionReadModel model, CancellationToken ct = default)
    {
        var existing = await context.Transactions.FindAsync([model.TransactionId], ct);
        if (existing is null)
        {
            context.Transactions.Add(model);
        }
        else
        {
            existing.UserId          = model.UserId;
            existing.AccountId       = model.AccountId;
            existing.CategoryId      = model.CategoryId;
            existing.TransactionType = model.TransactionType;
            existing.Amount          = model.Amount;
            existing.CurrencyCode    = model.CurrencyCode;
            existing.TransactionDate = model.TransactionDate;
            existing.SyncedAt        = model.SyncedAt;
        }
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid transactionId, CancellationToken ct = default)
    {
        var existing = await context.Transactions.FindAsync([transactionId], ct);
        if (existing is not null)
        {
            context.Transactions.Remove(existing);
            await context.SaveChangesAsync(ct);
        }
    }
}
