using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Shared.Infrastructure.Persistence;

/// <summary>
/// A read-only wrapper around a <typeparamref name="TContext"/> that:
/// <list type="bullet">
///   <item>Forces <c>AsNoTracking</c> on all queries (cheaper, no change tracking)</item>
///   <item>Throws on <c>SaveChanges</c> / <c>SaveChangesAsync</c> to prevent accidental writes</item>
/// </list>
///
/// Usage — register alongside the write context:
/// <code>
///   services.AddReadOnlyDbContext&lt;TransactionsDbContext&gt;(configuration);
/// </code>
/// Then inject <c>ReadOnlyDbContext&lt;TransactionsDbContext&gt;</c> in query handlers.
/// Item 10: Read-only DbContext for CQRS Query side
/// </summary>
public sealed class ReadOnlyDbContext<TContext> where TContext : DbContext
{
    private readonly TContext _inner;

    public ReadOnlyDbContext(TContext inner)
    {
        _inner = inner;
        _inner.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        _inner.ChangeTracker.AutoDetectChangesEnabled = false;
        _inner.ChangeTracker.LazyLoadingEnabled = false;
    }

    /// <summary>Exposes the underlying context for building LINQ queries (read-only).</summary>
    public TContext Context => _inner;

    public IQueryable<TEntity> Set<TEntity>() where TEntity : class
        => _inner.Set<TEntity>().AsNoTracking();

    // Prevent writes
    public int SaveChanges()
        => throw new InvalidOperationException(
            $"{nameof(ReadOnlyDbContext<TContext>)} is read-only. Use {typeof(TContext).Name} for writes.");

    public Task<int> SaveChangesAsync(CancellationToken _ = default)
        => throw new InvalidOperationException(
            $"{nameof(ReadOnlyDbContext<TContext>)} is read-only. Use {typeof(TContext).Name} for writes.");
}
