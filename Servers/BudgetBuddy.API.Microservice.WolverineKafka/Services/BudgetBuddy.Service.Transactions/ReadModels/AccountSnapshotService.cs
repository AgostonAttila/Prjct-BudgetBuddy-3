using System.Text.Json;
using BudgetBuddy.Shared.Kernel.Exceptions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace BudgetBuddy.Service.Transactions.ReadModels;

public class AccountSnapshotService : IAccountSnapshotService
{
    private readonly IMemoryCache _l1;
    private readonly IDistributedCache _l2;
    private readonly IAccountSnapshotRepository _l3;
    private readonly ILogger<AccountSnapshotService> _logger;

    public AccountSnapshotService(
        IMemoryCache l1,
        IDistributedCache l2,
        IAccountSnapshotRepository l3,
        ILogger<AccountSnapshotService> logger)
    {
        _l1 = l1; _l2 = l2; _l3 = l3; _logger = logger;
    }

    public async Task<AccountSnapshot> GetOrThrowAsync(Guid accountId, string userId, CancellationToken ct)
    {
        var cacheKey = $"acc:{accountId}";

        // L1 — in-memory (nanoseconds)
        if (_l1.TryGetValue(cacheKey, out AccountSnapshot? l1Hit) && l1Hit is not null)
        {
            return ValidateOwnership(l1Hit, userId);
        }

        // L2 — Redis (microseconds)
        var l2Raw = await _l2.GetStringAsync(cacheKey, ct);
        if (l2Raw is not null)
        {
            var l2Hit = JsonSerializer.Deserialize<AccountSnapshot>(l2Raw)!;
            _l1.Set(cacheKey, l2Hit, TimeSpan.FromSeconds(30));
            return ValidateOwnership(l2Hit, userId);
        }

        // L3 — local PostgreSQL read model (milliseconds)
        var l3Hit = await _l3.FindAsync(accountId, ct);
        if (l3Hit is not null)
        {
            await PopulateAsync(l3Hit, ct);
            return ValidateOwnership(l3Hit, userId);
        }

        _logger.LogWarning(
            "Account {AccountId} not found in any cache layer — compacted topic may be lagging",
            accountId);
        throw new NotFoundException("Account", accountId);
    }

    public async Task PopulateAsync(AccountSnapshot snapshot, CancellationToken ct)
    {
        var key = $"acc:{snapshot.AccountId}";
        var json = JsonSerializer.Serialize(snapshot);
        await _l2.SetStringAsync(key, json,
            new DistributedCacheEntryOptions
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }, ct);
        _l1.Set(key, snapshot, TimeSpan.FromSeconds(30));
    }

    public async Task InvalidateAsync(Guid accountId, CancellationToken ct)
    {
        var key = $"acc:{accountId}";
        _l1.Remove(key);
        await _l2.RemoveAsync(key, ct);
    }

    private static AccountSnapshot ValidateOwnership(AccountSnapshot snap, string userId)
    {
        if (snap.UserId != userId)
        {
            throw new DomainException("Account does not belong to the current user");
        }

        if (!snap.IsActive)
        {
            throw new DomainException("Account is inactive");
        }

        return snap;
    }
}
