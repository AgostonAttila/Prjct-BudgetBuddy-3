using System.Collections.Concurrent;

namespace BudgetBuddy.Shared.Infrastructure.Messaging;

/// <summary>
/// Singleton that maps Wolverine message CLR types to the schema versions declared by
/// their handler via <see cref="SupportedSchemaVersionsAttribute"/>.
///
/// Populated at Wolverine startup by <see cref="SchemaVersionPolicy"/> (before any message
/// is processed). Queried at runtime by <see cref="SchemaVersionMiddleware"/>.
/// </summary>
public sealed class SchemaVersionRegistry
{
    private readonly ConcurrentDictionary<Type, string[]> _map = new();

    /// <summary>Called by <see cref="SchemaVersionPolicy"/> during startup.</summary>
    internal void Register(Type messageType, string[] supportedVersions)
        => _map[messageType] = supportedVersions;

    /// <summary>
    /// Returns the supported versions for <paramref name="messageType"/>,
    /// or <c>null</c> if the message type has no version constraint registered.
    /// </summary>
    public string[]? GetSupportedVersions(Type messageType)
        => _map.TryGetValue(messageType, out var versions) ? versions : null;
}
