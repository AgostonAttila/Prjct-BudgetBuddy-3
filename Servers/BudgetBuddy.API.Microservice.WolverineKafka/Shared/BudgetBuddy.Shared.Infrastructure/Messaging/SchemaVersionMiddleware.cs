using Wolverine;

namespace BudgetBuddy.Shared.Infrastructure.Messaging;

/// <summary>
/// Wolverine handler middleware that validates the <c>schema-version</c> Kafka header
/// against the versions declared by the handler's <see cref="SupportedSchemaVersionsAttribute"/>.
///
/// Behaviour (mirrors KafkaConsumerBase A-02):
///   • No <c>schema-version</c> header → pass through (pre-versioned producers).
///   • Version not in the supported list → WARNING logged, message still processed
///     (forward-compatible: newer producers can add fields handlers ignore).
///   • Version in the supported list → silent pass through.
///
/// Injected by <see cref="SchemaVersionPolicy"/> only into handler chains where the
/// handler class carries <see cref="SupportedSchemaVersionsAttribute"/>.
/// </summary>
public sealed class SchemaVersionMiddleware(
    SchemaVersionRegistry registry,
    ILogger<SchemaVersionMiddleware> logger)
{
    public HandlerContinuation Before(Envelope envelope)
    {
        if (!envelope.Headers.TryGetValue("schema-version", out var receivedVersion))
        {
            return HandlerContinuation.Continue;
        }

        if (envelope.Message is null)
        {
            return HandlerContinuation.Continue;
        }

        var supported = registry.GetSupportedVersions(envelope.Message.GetType());
        if (supported is null)
        {
            return HandlerContinuation.Continue;
        }

        if (!Array.Exists(supported, v => v == receivedVersion))
        {
            logger.LogWarning(
                "Received {MessageType} with schema version '{Received}' — this handler supports [{Supported}]. " +
                "The producer likely has a newer schema version (A-01 schema evolution guard).",
                envelope.Message.GetType().Name,
                receivedVersion,
                string.Join(", ", supported));
        }

        return HandlerContinuation.Continue;
    }
}
