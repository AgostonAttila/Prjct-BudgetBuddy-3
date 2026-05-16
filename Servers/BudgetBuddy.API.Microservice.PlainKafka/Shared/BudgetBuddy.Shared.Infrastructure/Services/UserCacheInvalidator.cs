using Microsoft.Extensions.Caching.Hybrid;

namespace BudgetBuddy.Shared.Infrastructure.Services;

public class UserCacheInvalidator(HybridCache cache) : IUserCacheInvalidator
{
    public Task InvalidateAsync(string userId, CancellationToken ct = default)
        => cache.RemoveByTagAsync($"user:{userId}", ct).AsTask();
}
