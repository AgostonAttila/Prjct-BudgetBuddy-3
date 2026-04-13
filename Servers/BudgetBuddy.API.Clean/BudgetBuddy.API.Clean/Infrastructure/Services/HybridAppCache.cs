using BudgetBuddy.Application.Common.Contracts;
using Microsoft.Extensions.Caching.Hybrid;

namespace BudgetBuddy.Infrastructure.Services;

public class HybridAppCache(HybridCache hybridCache) : IAppCache
{
    public Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        AppCacheOptions options,
        IReadOnlyList<string>? tags = null,
        CancellationToken ct = default)
        => hybridCache.GetOrCreateAsync(
            key,
            factory,
            new HybridCacheEntryOptions
            {
                Expiration = options.Ttl,
                LocalCacheExpiration = options.LocalTtl
            },
            tags: tags,
            cancellationToken: ct).AsTask();

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => hybridCache.RemoveAsync(key, ct).AsTask();
}
