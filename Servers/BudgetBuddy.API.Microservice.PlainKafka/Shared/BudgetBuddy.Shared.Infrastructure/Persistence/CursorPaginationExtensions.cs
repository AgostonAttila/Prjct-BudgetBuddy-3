using System.Text;
using System.Text.Json;
using BudgetBuddy.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Shared.Infrastructure.Persistence;

/// <summary>
/// P-02: EF Core extensions for cursor-based pagination.
///
/// The cursor is an opaque base64-encoded JSON payload that encodes the last-seen ordering key.
/// Supported key types: <see cref="Guid"/>, <see cref="DateTime"/>, <see cref="long"/>, <see cref="string"/>.
///
/// Requirements:
/// - The query MUST be deterministically ordered (e.g., OrderBy CreatedAt, then Id).
/// - The cursor key must match the last field in the OrderBy chain (used as the seek predicate).
/// </summary>
public static class CursorPaginationExtensions
{
    private const int MaxPageSize = 200;

    /// <summary>
    /// Applies keyset (seek) pagination to an <see cref="IQueryable{T}"/> ordered by <see cref="Guid"/> id.
    /// The query must already be ordered by the same property used as the cursor key.
    /// </summary>
    public static async Task<CursorPage<T>> ToCursorPageByIdAsync<T>(
        this IQueryable<T> query,
        Func<T, Guid> idSelector,
        string? cursor,
        int pageSize,
        CancellationToken ct = default)
        where T : class
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        if (cursor is not null)
        {
            var lastId = DecodeCursor<Guid>(cursor);
            // Seek past the last seen id — relies on the caller having ordered by Id ascending.
            query = query.Where(e => EF.Property<Guid>(e, "Id").CompareTo(lastId) > 0);
        }

        // Fetch one extra to detect whether a next page exists.
        var items = await query.Take(pageSize + 1).ToListAsync(ct);

        string? nextCursor = null;
        if (items.Count > pageSize)
        {
            items.RemoveAt(items.Count - 1);
            nextCursor = EncodeCursor(idSelector(items[^1]));
        }

        return new CursorPage<T> { Items = items, NextCursor = nextCursor };
    }

    // ── Cursor encoding ──────────────────────────────────────────────────────

    public static string EncodeCursor<TKey>(TKey key) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(key)));

    public static TKey DecodeCursor<TKey>(string cursor)
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        return JsonSerializer.Deserialize<TKey>(json)!;
    }
}
