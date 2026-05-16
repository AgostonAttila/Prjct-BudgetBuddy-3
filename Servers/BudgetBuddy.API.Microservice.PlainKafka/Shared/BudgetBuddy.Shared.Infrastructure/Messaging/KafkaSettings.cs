using Confluent.Kafka;

namespace BudgetBuddy.Shared.Infrastructure.Messaging;

public class KafkaSettings
{
    public string BootstrapServers    { get; set; } = "localhost:9092";
    /// <summary>
    /// How long the broker waits for a heartbeat before considering the consumer dead.
    /// Raised from 10 s to 30 s to tolerate GC pauses and slow processing without
    /// triggering unnecessary partition revocations (finding #8).
    /// Must be: SessionTimeoutMs > HeartbeatIntervalMs * 3 (broker enforces this).
    /// </summary>
    public int    SessionTimeoutMs    { get; set; } = 30_000;
    public int    HeartbeatIntervalMs { get; set; } = 3_000;
    public int    FetchWaitMaxMs      { get; set; } = 500;
    /// <summary>
    /// Maximum time between poll() calls before the consumer is evicted from the group.
    /// Should exceed the worst-case processing time for a single message batch.
    /// Kafka default is 300 000 ms (5 min).
    /// </summary>
    public int    MaxPollIntervalMs   { get; set; } = 300_000;

    // ─── Statistics ─────────────────────────────────────────────────────────
    /// <summary>
    /// How often librdkafka emits statistics (in ms). Used by the statistics handler
    /// to derive consumer lag and producer queue depth for Prometheus.
    /// Default: 60 000 ms (1 min). Lower values (e.g. 10 000) give finer lag resolution
    /// at the cost of slightly more CPU.
    /// </summary>
    public int StatisticsIntervalMs { get; set; } = 60_000;

    // ─── Retry / DLQ ────────────────────────────────────────────────────────
    /// <summary>
    /// How many consecutive failures on the same partition+offset before the message is
    /// routed to the DLQ (or logged as CRITICAL if no DLQ is configured).
    /// </summary>
    public int MaxConsumerRetries { get; set; } = 5;

    // ─── Circuit breaker ────────────────────────────────────────────────────
    /// <summary>
    /// How many consecutive message-processing failures trigger the circuit breaker (finding #9).
    /// After this many back-to-back failures the consumer pauses for
    /// <see cref="CircuitBreakerCooldownSeconds"/> before retrying.
    /// </summary>
    public int CircuitBreakerConsecutiveFailures { get; set; } = 5;
    /// <summary>How long the circuit breaker stays open before attempting half-open recovery.</summary>
    public int CircuitBreakerCooldownSeconds     { get; set; } = 30;

    // ─── Security (TLS / SASL) ──────────────────────────────────────────────
    // Defaults to Plaintext (dev). Override in appsettings.Production.json / env vars.
    // Valid SecurityProtocol values: Plaintext | Ssl | SaslPlaintext | SaslSsl
    // Valid SaslMechanism values: Plain | ScramSha256 | ScramSha512 | OAuthBearer
    //
    // Production credential supply options (appsettings.Production.json leaves these empty):
    //   1. Environment variables  — KAFKA__SASLPASSWORD=secret  (ASP.NET Core __ separator)
    //   2. Azure Key Vault        — secrets named Kafka--SaslPassword (-- separator)
    //   3. Docker / K8s secrets   — mounted as env vars or files consumed by a config provider
    // Never commit real credentials to source control.
    public string  SecurityProtocol      { get; set; } = "Plaintext";
    public string? SaslMechanism         { get; set; }
    public string? SaslUsername          { get; set; }
    public string? SaslPassword          { get; set; }
    public string? SslCaLocation         { get; set; }
    public string? SslCertificateLocation { get; set; }
    public string? SslKeyLocation        { get; set; }
    /// <summary>
    /// Explicit hostname verification algorithm for SSL connections.
    /// "https" = standard CN/SAN hostname check (recommended for Confluent Cloud, MSK).
    /// "none" = disable hostname verification (only for dev/test).
    /// Defaults to "https" to protect against MitM attacks on managed Kafka services.
    /// </summary>
    public string SslEndpointIdentificationAlgorithm { get; set; } = "https";

    /// <summary>
    /// Applies TLS/SASL security settings to any Confluent.Kafka ClientConfig
    /// (ProducerConfig, ConsumerConfig, AdminClientConfig all inherit ClientConfig).
    /// No-op when SecurityProtocol is Plaintext.
    /// </summary>
    public void ApplySecurity(ClientConfig config)
    {
        if (!Enum.TryParse<SecurityProtocol>(SecurityProtocol, ignoreCase: true, out var protocol))
        {
            throw new InvalidOperationException(
                $"Invalid Kafka SecurityProtocol '{SecurityProtocol}'. " +
                "Valid values: Plaintext, Ssl, SaslPlaintext, SaslSsl");
        }

        config.SecurityProtocol = protocol;

        if (protocol == Confluent.Kafka.SecurityProtocol.Plaintext)
        {
            return;
        }

        if (!string.IsNullOrEmpty(SslEndpointIdentificationAlgorithm))
        {
            config.SslEndpointIdentificationAlgorithm =
                SslEndpointIdentificationAlgorithm.Equals("none", StringComparison.OrdinalIgnoreCase)
                    ? Confluent.Kafka.SslEndpointIdentificationAlgorithm.None
                    : Confluent.Kafka.SslEndpointIdentificationAlgorithm.Https;
        }

        if (SslCaLocation is not null)
        {
            config.SslCaLocation          = SslCaLocation;
        }

        if (SslCertificateLocation is not null)
        {
            config.SslCertificateLocation = SslCertificateLocation;
        }

        if (SslKeyLocation is not null)
        {
            config.SslKeyLocation         = SslKeyLocation;
        }

        if (SaslMechanism is not null)
        {
            if (!Enum.TryParse<Confluent.Kafka.SaslMechanism>(SaslMechanism, ignoreCase: true, out var mechanism))
            {
                throw new InvalidOperationException(
                    $"Invalid Kafka SaslMechanism '{SaslMechanism}'. " +
                    "Valid values: Plain, ScramSha256, ScramSha512, OAuthBearer");
            }

            config.SaslMechanism = mechanism;
            config.SaslUsername  = SaslUsername;
            config.SaslPassword  = SaslPassword;
        }
    }
}
