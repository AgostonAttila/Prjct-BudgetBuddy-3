namespace BudgetBuddy.API.VSA.Common.Shared.Contracts;

public interface IUserCacheInvalidator
{
    Task InvalidateAsync(string userId, CancellationToken ct = default);
}
