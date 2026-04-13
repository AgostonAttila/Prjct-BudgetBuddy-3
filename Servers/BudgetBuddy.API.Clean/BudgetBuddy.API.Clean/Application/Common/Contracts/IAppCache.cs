namespace BudgetBuddy.Application.Common.Contracts;

/// <summary>Cache entry lifetime options. Ttl = L2 (distributed), LocalTtl = L1 (in-memory).</summary>
public record AppCacheOptions(TimeSpan Ttl, TimeSpan? LocalTtl = null);

public interface IAppCache
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        AppCacheOptions options,
        IReadOnlyList<string>? tags = null,
        CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);
}
