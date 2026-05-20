namespace BudgetBuddy.Shared.Infrastructure.Messaging;

/// <summary>
/// Declares which Kafka message schema versions this Wolverine handler accepts (A-02).
/// Mirrors the <c>SupportedVersions</c> property on the legacy KafkaConsumerBase.
///
/// At startup, <see cref="SchemaVersionPolicy"/> reads this attribute and registers the
/// message-type → versions mapping in <see cref="SchemaVersionRegistry"/>.
/// At runtime, <see cref="SchemaVersionMiddleware"/> checks the incoming <c>schema-version</c>
/// Kafka header and logs a warning when a mismatch is detected — processing always continues
/// (forward-compatible strategy).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SupportedSchemaVersionsAttribute : Attribute
{
    public SupportedSchemaVersionsAttribute(params string[] supportedVersions)
        => SupportedVersions = supportedVersions;

    public string[] SupportedVersions { get; }
}
