namespace BudgetBuddy.Application.Common.Contracts;

public interface IUserCacheInvalidator
{
    Task InvalidateAsync(string userId, CancellationToken ct = default);
}
