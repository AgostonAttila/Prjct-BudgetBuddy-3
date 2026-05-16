using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BudgetBuddy.Shared.Messages.Integration;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Shared.Infrastructure.Messaging;

/// <summary>
/// Base class for Kafka consumers. Handles retry, metrics, structured logging,
/// and poison-pill protection (messages that always fail are sent to DLQ after
/// <see cref="MaxConsumerRetries"/> consecutive failures and then committed).
/// Override ProcessAsync to implement message handling logic.
/// Override DlqTopic to enable poison-pill DLQ (defaults to null = log only).
/// Override SupportedVersions to enforce schema version compatibility (A-02).
/// </summary>
public abstract class KafkaConsumerBase<TMessage> : BackgroundService
    where TMessage : IIntegrationEvent
{
    // O-01: ActivitySource for distributed trace propagation across Kafka boundaries.
#pragma warning disable S2743
    private static readonly ActivitySource KafkaActivitySource = new("BudgetBuddy.Kafka");
#pragma warning restore S2743

    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    protected readonly ILogger Logger;
    private readonly string _topic;
    private readonly string _consumerGroup;
    private readonly int _maxConsumerRetries;

    // Tracks consecutive failure count per partition+offset to detect poison-pill messages.
    private readonly ConcurrentDictionary<string, int> _failedOffsets = new();

    // ── Circuit breaker state (finding #9) ───────────────────────────────────
    // Protects downstream services (DB, HTTP) from being flooded when they are
    // consistently unavailable. Single-threaded (consumer loop), no locking needed.
    private readonly int _circuitBreakerThreshold;
    private readonly TimeSpan _circuitCooldown;
    private int _consecutiveFailures;
    private DateTime _circuitOpenedAt = DateTime.MinValue;

    protected KafkaConsumerBase(
        KafkaSettings settings,
        string topic,
        string consumerGroup,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        _topic               = topic;
        _consumerGroup       = consumerGroup;
        _scopeFactory        = scopeFactory;
        Logger               = logger;
        _maxConsumerRetries  = settings.MaxConsumerRetries;

        _circuitBreakerThreshold = settings.CircuitBreakerConsecutiveFailures;
        _circuitCooldown         = TimeSpan.FromSeconds(settings.CircuitBreakerCooldownSeconds);

        var config = new ConsumerConfig
        {
            BootstrapServers      = settings.BootstrapServers,
            GroupId               = consumerGroup,
            AutoOffsetReset       = AutoOffsetReset.Earliest,
            EnableAutoCommit      = false,
            EnableAutoOffsetStore = false,
            SessionTimeoutMs      = settings.SessionTimeoutMs,
            HeartbeatIntervalMs   = settings.HeartbeatIntervalMs,
            FetchWaitMaxMs        = settings.FetchWaitMaxMs,
            MaxPollIntervalMs     = settings.MaxPollIntervalMs,
            StatisticsIntervalMs  = settings.StatisticsIntervalMs,
        };
        settings.ApplySecurity(config);

        _consumer = new ConsumerBuilder<string, string>(config)
            // Item 18: Kafka statistics → Prometheus consumer lag gauge
            .SetStatisticsHandler((_, json) => KafkaMetrics.RecordStatistics(json))
            // Item 19: commit current offsets before partitions are revoked (graceful rebalance)
            .SetPartitionsRevokedHandler((c, partitions) =>
            {
                try { c.Commit(partitions); }
                catch { /* nothing to commit if no messages were processed */ }
                logger.LogInformation(
                    "Consumer {Group}: partitions revoked {Partitions} — offsets committed",
                    consumerGroup, string.Join(",", partitions.Select(p => $"{p.Topic}[{p.Partition}]")));
            })
            // Item 19: partitions lost (e.g. session timeout) — cannot commit, just log
            .SetPartitionsLostHandler((_, partitions) =>
                logger.LogWarning(
                    "Consumer {Group}: partitions LOST (session timeout?) {Partitions}",
                    consumerGroup, string.Join(",", partitions.Select(p => $"{p.Topic}[{p.Partition}]"))))
            .Build();
    }

    /// <summary>
    /// DLQ topic for poison-pill messages. Return a non-null value to enable DLQ routing
    /// after <see cref="MaxConsumerRetries"/> consecutive failures on the same offset.
    /// </summary>
    protected virtual string? DlqTopic => null;

    /// <summary>
    /// Schema versions this consumer can handle (A-02 version guard).
    /// Messages with other versions are processed but logged as a warning.
    /// Override in subclasses to declare compatibility range.
    /// </summary>
    protected virtual string[] SupportedVersions => ["1.0"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topic);
        Logger.LogInformation("Consumer {Group} subscribed to {Topic}", _consumerGroup, _topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            // ── Circuit breaker check (finding #9) ───────────────────────────
            if (_consecutiveFailures >= _circuitBreakerThreshold)
            {
                var elapsed = DateTime.UtcNow - _circuitOpenedAt;
                if (elapsed < _circuitCooldown)
                {
                    Logger.LogDebug(
                        "Circuit breaker OPEN for consumer {Group}/{Topic} — cooldown {Remaining:0}s remaining",
                        _consumerGroup, _topic, (_circuitCooldown - elapsed).TotalSeconds);
                    await Task.Delay(1_000, stoppingToken);
                    continue;
                }
                // Cooldown expired → half-open: reset counter and attempt one message
                Logger.LogInformation(
                    "Circuit breaker HALF-OPEN for consumer {Group}/{Topic} — attempting recovery",
                    _consumerGroup, _topic);
                _consecutiveFailures = 0;
            }
            // ─────────────────────────────────────────────────────────────────

            ConsumeResult<string, string>? result = null;
            try
            {
                result = _consumer.Consume(stoppingToken);

                // Item 17: extract correlation-id from Kafka message header and push to log context
                var correlationId = result.Message.Headers
                    .TryGetLastBytes("correlation-id", out var cidBytes)
                        ? Encoding.UTF8.GetString(cidBytes)
                        : Guid.NewGuid().ToString();

                // O-01: restore distributed trace from W3C traceparent header
                var parentContext = ExtractTraceContext(result.Message.Headers);

                using var activity = KafkaActivitySource.StartActivity(
                    $"kafka.consume {_topic}",
                    ActivityKind.Consumer,
                    parentContext);
                activity?.SetTag("messaging.system", "kafka");
                activity?.SetTag("messaging.destination", _topic);
                activity?.SetTag("messaging.consumer_group", _consumerGroup);

                using var logScope = Logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["Topic"]         = _topic,
                    ["Partition"]     = result.Partition.Value,
                    ["Offset"]        = result.Offset.Value,
                });

                // A-02: version compatibility guard
                ValidateVersion(result.Message.Headers);

                var message = JsonSerializer.Deserialize<TMessage>(result.Message.Value)
                    ?? throw new InvalidOperationException("Null message deserialized");

                // A-01: schema evolution guard — warn on unknown fields from a newer producer
                WarnIfUnknownFields(result.Message.Value);

                var sw = Stopwatch.StartNew();
                await using var scope = _scopeFactory.CreateAsyncScope();
                await ProcessAsync(message, scope.ServiceProvider, stoppingToken);
                sw.Stop();

                _consumer.Commit(result);
                _failedOffsets.TryRemove(OffsetKey(result), out _);
                _consecutiveFailures = 0; // circuit breaker: reset on success

                KafkaMetrics.MessagesConsumed.Add(1,
                    new KeyValuePair<string, object?>("topic", _topic),
                    new KeyValuePair<string, object?>("group", _consumerGroup));
                KafkaMetrics.ProcessingDuration.Record(sw.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("topic", _topic));

                Logger.LogDebug(
                    "Processed {MessageType} from {Topic} partition {Partition} offset {Offset}",
                    typeof(TMessage).Name, _topic,
                    result.Partition.Value, result.Offset.Value);
            }
            catch (ConsumeException ex)
            {
                Logger.LogError(ex, "Kafka consume error on {Topic}", _topic);
                KafkaMetrics.MessagesFailed.Add(1,
                    new KeyValuePair<string, object?>("topic", _topic));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing message from {Topic}", _topic);
                KafkaMetrics.MessagesFailed.Add(1,
                    new KeyValuePair<string, object?>("topic", _topic));

                // Circuit breaker: count consecutive failures (finding #9)
                _consecutiveFailures++;
                if (_consecutiveFailures == _circuitBreakerThreshold)
                {
                    _circuitOpenedAt = DateTime.UtcNow;
                    Logger.LogWarning(
                        "Circuit breaker OPEN for consumer {Group}/{Topic} after {Failures} consecutive failures. " +
                        "Will pause for {Cooldown}s before retrying.",
                        _consumerGroup, _topic, _consecutiveFailures, _circuitCooldown.TotalSeconds);
                }

                if (result is not null)
                {
                    await HandlePoisonPillAsync(result, ex, stoppingToken);

                    // Exponential backoff before the next retry poll so transient failures
                    // (e.g. DB unavailable) don't spin at full speed. Delay = 200ms * 2^(attempts-1),
                    // capped at 10 s. The circuit breaker handles sustained outages beyond that.
                    if (_failedOffsets.TryGetValue(OffsetKey(result), out var attempts))
                    {
                        var backoffMs = Math.Min(200 * (1 << Math.Min(attempts - 1, 5)), 10_000);
                        await Task.Delay(backoffMs, stoppingToken);
                    }
                }
                // Don't commit on failure — message will be retried on next poll unless sent to DLQ.
            }
        }

        _consumer.Close();
    }

    private async Task HandlePoisonPillAsync(
        ConsumeResult<string, string> result, Exception ex, CancellationToken ct)
    {
        var key      = OffsetKey(result);
        var attempts = _failedOffsets.AddOrUpdate(key, 1, (_, v) => v + 1);

        if (attempts < _maxConsumerRetries)
        {
            return;
        }

        _failedOffsets.TryRemove(key, out _);

        if (DlqTopic is null)
        {
            Logger.LogCritical(
                "Poison pill message on {Topic} partition {Partition} offset {Offset} failed {Attempts} times — " +
                "no DLQ configured, partition will remain blocked. Override DlqTopic to enable DLQ routing.",
                _topic, result.Partition.Value, result.Offset.Value, attempts);
            return;
        }

        try
        {
            var dlqPayload = new
            {
                OriginalTopic = _topic,
                ConsumerGroup = _consumerGroup,
                Partition     = result.Partition.Value,
                Offset        = result.Offset.Value,
                MessageKey    = result.Message.Key,
                Payload       = result.Message.Value,
                Error         = ex.Message,
                Attempts      = attempts,
                FailedAt      = DateTime.UtcNow,
            };

            await using var scope = _scopeFactory.CreateAsyncScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishStateAsync(DlqTopic, result.Message.Key, dlqPayload, ct);

            _consumer.Commit(result);

            KafkaMetrics.MessagesSentToDlq.Add(1,
                new KeyValuePair<string, object?>("service", _consumerGroup),
                new KeyValuePair<string, object?>("event_type", typeof(TMessage).Name));

            Logger.LogCritical(
                "Poison pill message from {Topic} partition {Partition} offset {Offset} sent to DLQ {DlqTopic} after {Attempts} retries. Error: {Error}",
                _topic, result.Partition.Value, result.Offset.Value, DlqTopic, attempts, ex.Message);
        }
        catch (Exception dlqEx)
        {
            Logger.LogCritical(dlqEx,
                "CRITICAL: Failed to send poison pill from {Topic} partition {Partition} offset {Offset} to DLQ {DlqTopic}. Manual intervention required.",
                _topic, result.Partition.Value, result.Offset.Value, DlqTopic);
        }
    }

    private static string OffsetKey(ConsumeResult<string, string> result)
        => $"{result.Partition.Value}:{result.Offset.Value}";

    protected abstract Task ProcessAsync(TMessage message, IServiceProvider services, CancellationToken ct);

    /// <summary>
    /// Helper for subclasses that need explicit database transaction isolation.
    /// </summary>
    protected static async Task ProcessInTransactionAsync<TDbContext>(
        IServiceProvider services,
        Func<TDbContext, Task> work,
        System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted,
        CancellationToken ct = default)
        where TDbContext : DbContext
    {
        var db = services.GetRequiredService<TDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(isolationLevel, ct);
        try
        {
            await work(db);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ── O-01: Distributed trace helpers ──────────────────────────────────────

    private static ActivityContext ExtractTraceContext(Headers headers)
    {
        try
        {
            var traceparentBytes = headers.FirstOrDefault(h => h.Key == "traceparent")?.GetValueBytes();
            if (traceparentBytes is null)
            {
                return default;
            }

            var tracestateBytes = headers.FirstOrDefault(h => h.Key == "tracestate")?.GetValueBytes();
            var tracestate      = tracestateBytes != null ? Encoding.UTF8.GetString(tracestateBytes) : null;

            return ActivityContext.TryParse(
                Encoding.UTF8.GetString(traceparentBytes), tracestate, isRemote: true, out var ctx)
                ? ctx
                : default;
        }
        catch
        {
            return default;
        }
    }

    // ── A-02: Schema version guard ────────────────────────────────────────────

    private void ValidateVersion(Headers headers)
    {
        try
        {
            var versionBytes = headers.FirstOrDefault(h => h.Key == "version")?.GetValueBytes();
            if (versionBytes is null)
            {
                return;
            }

            var version = Encoding.UTF8.GetString(versionBytes);
            if (!SupportedVersions.Contains(version))
            {
                Logger.LogWarning(
                    "Received {MessageType} with schema version '{Version}' — this consumer supports [{Supported}]. " +
                    "The producer may have a newer schema. Proceeding with best-effort deserialization.",
                    typeof(TMessage).Name, version, string.Join(", ", SupportedVersions));
            }
        }
        catch { /* best-effort */ }
    }

    // ── A-01: Unknown field guard ─────────────────────────────────────────────

#pragma warning disable S2743
    private static readonly ConcurrentDictionary<Type, HashSet<string>> KnownPropsCache = new();
#pragma warning restore S2743

    private void WarnIfUnknownFields(string json)
    {
        try
        {
            using var doc  = JsonDocument.Parse(json);
            var knownProps = KnownPropsCache.GetOrAdd(typeof(TMessage), t =>
                t.GetProperties()
                    .Select(p => p.Name.ToLowerInvariant())
                    .ToHashSet());

            foreach (var propName in doc.RootElement.EnumerateObject()
                .Select(prop => prop.Name)
                .Where(name => !knownProps.Contains(name.ToLowerInvariant())))
            {
                Logger.LogWarning(
                    "Unknown field '{Field}' in {MessageType} payload — " +
                    "the producer likely has a newer schema version (A-01 schema evolution guard).",
                    propName, typeof(TMessage).Name);
            }
        }
        catch { /* best-effort */ }
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}
