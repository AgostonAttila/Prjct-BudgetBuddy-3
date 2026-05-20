namespace BudgetBuddy.Shared.Kernel.Pagination;

/// <summary>
/// P-02: Cursor-based pagination result. Prefer over offset pagination for large or
/// frequently-updated datasets — cursor pagination is stable under concurrent inserts/deletes
/// and does not degrade with large page offsets.
///
/// Usage:
/// <code>
/// var page = await db.Transactions
///     .Where(t => t.UserId == userId)
///     .OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)
///     .ToCursorPageAsync(cursor: request.Cursor, pageSize: request.PageSize, ct);
/// </code>
/// </summary>
/// <typeparam name="T">Projected item type.</typeparam>
public record CursorPage<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>
    /// Opaque cursor pointing to the next page.
    /// Pass this value as <c>cursor</c> on the next request.
    /// <see langword="null"/> when this is the last page.
    /// </summary>
    public string? NextCursor { get; init; }

    /// <summary>Whether a next page exists.</summary>
    public bool HasMore => NextCursor is not null;

    /// <summary>Total items returned on this page (convenience).</summary>
    public int Count => Items.Count;
}

/// <summary>Pagination request using an opaque cursor.</summary>
public record CursorRequest
{
    /// <summary>Cursor from the previous response. <see langword="null"/> to start from the beginning.</summary>
    public string? Cursor { get; init; }

    /// <summary>Items per page. Capped at 200 server-side.</summary>
    public int PageSize { get; init; } = 50;
}
