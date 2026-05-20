namespace BudgetBuddy.Shared.Infrastructure;

public interface IUserCacheInvalidator
{
    Task InvalidateAsync(string userId, CancellationToken ct = default);
}
