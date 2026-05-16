using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Shared.Infrastructure.Messaging;

/// <summary>
/// Base class for Kafka consumers that subscribe to multiple topics simultaneously.
/// Provides the same infrastructure guarantees as <see cref="KafkaConsumerBase{TMessage}"/>:
/// statistics handler (consumer lag metrics), graceful rebalancing, correlation-ID log scope,
/// per-message metrics, and poison-pill protection.
///
/// Usage: derive, pass topic array + consumer group, implement
/// <see cref="ProcessAsync(string, string?, IServiceProvider, CancellationToken)"/>.
/// Override <see cref="DlqTopic"/> to enable DLQ routing for poison-pill messages.
/// </summary>
public abstract class KafkaMultiTopicConsumerBase : BackgroundService
{
    // O-01: ActivitySource for distributed trace propagation across Kafka boundaries.
    private static readonly ActivitySource KafkaActivitySource = new("BudgetBuddy.Kafka");

    private readonly IConsumer<string, string?> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    protected readonly ILogger Logger;
    private readonly string[] _topics;
    private readonly string _consumerGroup;
    private readonly int _maxConsumerRetries;

    private readonly ConcurrentDictionary<string, int> _failedOffsets = new();

    // ── Circuit breaker state (finding #9) ───────────────────────────────────
    private readonly int _circuitBreakerThreshold;
    private readonly TimeSpan _circuitCooldown;
    private int _consecutiveFailures;
    private DateTime _circuitOpenedAt = DateTime.MinValue;

    protected KafkaMultiTopicConsumerBase(
        KafkaSettings settings,
        string[] topics,
        string consumerGroup,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        _topics              = topics;
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

        _consumer = new ConsumerBuilder<string, string?>(config)
            .SetStatisticsHandler((_, json) => KafkaMetrics.RecordStatistics(json))
            .SetPartitionsRevokedHandler((c, partitions) =>
            {
                try { c.Commit(partitions); }
                catch { /* nothing to commit if no messages were processed */ }
                logger.LogInformation(
                    "Consumer {Group}: partitions revoked {Partitions} — offsets committed",
                    consumerGroup, string.Join(",", partitions.Select(p => $"{p.Topic}[{p.Partition}]")));
            })
            .SetPartitionsLostHandler((_, partitions) =>
                logger.LogWarning(
                    "Consumer {Group}: partitions LOST (session timeout?) {Partitions}",
                    consumerGroup, string.Join(",", partitions.Select(p => $"{p.Topic}[{p.Partition}]"))))
            .Build();
    }

    /// <summary>
    /// DLQ topic for poison-pill messages. When null (default), logs CRITICAL but keeps retrying.
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
        _consumer.Subscribe(_topics);
        Logger.LogInformation(
            "Consumer {Group} subscribed to topics: {Topics}",
            _consumerGroup, string.Join(", ", _topics));

        while (!stoppingToken.IsCancellationRequested)
        {
            // ── Circuit breaker check (finding #9) ───────────────────────────
            if (_consecutiveFailures >= _circuitBreakerThreshold)
            {
                var elapsed = DateTime.UtcNow - _circuitOpenedAt;
                if (elapsed < _circuitCooldown)
                {
                    Logger.LogDebug(
                        "Circuit breaker OPEN for consumer {Group} — cooldown {Remaining:0}s remaining",
                        _consumerGroup, (_circuitCooldown - elapsed).TotalSeconds);
                    await Task.Delay(1_000, stoppingToken);
                    continue;
                }
                Logger.LogInformation(
                    "Circuit breaker HALF-OPEN for consumer {Group} — attempting recovery", _consumerGroup);
                _consecutiveFailures = 0;
            }
            // ─────────────────────────────────────────────────────────────────

            ConsumeResult<string, string?>? result = null;
            try
            {
                result = _consumer.Consume(stoppingToken);

                var correlationId = result.Message.Headers
                    .TryGetLastBytes("correlation-id", out var cidBytes)
                        ? Encoding.UTF8.GetString(cidBytes)
                        : Guid.NewGuid().ToString();

                // O-01: restore distributed trace from W3C traceparent header
                var parentContext = ExtractTraceContext(result.Message.Headers);
                using var activity = KafkaActivitySource.StartActivity(
                    $"kafka.consume {result.Topic}",
                    ActivityKind.Consumer,
                    parentContext);
                activity?.SetTag("messaging.system", "kafka");
                activity?.SetTag("messaging.destination", result.Topic);
                activity?.SetTag("messaging.consumer_group", _consumerGroup);

                using var logScope = Logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["Topic"]         = result.Topic,
                    ["Partition"]     = result.Partition.Value,
                    ["Offset"]        = result.Offset.Value,
                });

                // A-02: version compatibility guard
                ValidateVersion(result.Message.Headers, result.Topic);

                var sw = Stopwatch.StartNew();
                await using var scope = _scopeFactory.CreateAsyncScope();
                await ProcessAsync(
                    result.Topic, result.Message.Value,
                    result.Message.Key, result.Offset.Value,
                    scope.ServiceProvider, stoppingToken);
                sw.Stop();

                _consumer.Commit(result);
                _failedOffsets.TryRemove(OffsetKey(result), out _);
                _consecutiveFailures = 0; // circuit breaker: reset on success

                KafkaMetrics.MessagesConsumed.Add(1,
                    new KeyValuePair<string, object?>("topic", result.Topic),
                    new KeyValuePair<string, object?>("group", _consumerGroup));
                KafkaMetrics.ProcessingDuration.Record(sw.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("topic", result.Topic));
            }
            catch (ConsumeException ex)
            {
                Logger.LogError(ex, "Kafka consume error in consumer group {Group}", _consumerGroup);
                KafkaMetrics.MessagesFailed.Add(1,
                    new KeyValuePair<string, object?>("topic", result?.Topic ?? _consumerGroup));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing message in consumer group {Group}", _consumerGroup);
                KafkaMetrics.MessagesFailed.Add(1,
                    new KeyValuePair<string, object?>("topic", result?.Topic ?? _consumerGroup));

                // Circuit breaker: count consecutive failures (finding #9)
                _consecutiveFailures++;
                if (_consecutiveFailures == _circuitBreakerThreshold)
                {
                    _circuitOpenedAt = DateTime.UtcNow;
                    Logger.LogWarning(
                        "Circuit breaker OPEN for consumer {Group} after {Failures} consecutive failures. " +
                        "Will pause for {Cooldown}s before retrying.",
                        _consumerGroup, _consecutiveFailures, _circuitCooldown.TotalSeconds);
                }

                if (result is not null)
                {
                    await HandlePoisonPillAsync(result, ex, stoppingToken);

                    // Exponential backoff before the next retry poll.
                    // Delay = 200ms * 2^(attempts-1), capped at 10 s.
                    if (_failedOffsets.TryGetValue(OffsetKey(result), out var attempts))
                    {
                        var backoffMs = Math.Min(200 * (1 << Math.Min(attempts - 1, 5)), 10_000);
                        await Task.Delay(backoffMs, stoppingToken);
                    }
                }
            }
        }

        _consumer.Close();
    }

    private async Task HandlePoisonPillAsync(
        ConsumeResult<string, string?> result, Exception ex, CancellationToken ct)
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
                "Poison pill in {Topic} partition {Partition} offset {Offset} failed {Attempts} times — " +
                "no DLQ configured. Override DlqTopic to enable DLQ routing.",
                result.Topic, result.Partition.Value, result.Offset.Value, attempts);
            return;
        }

        try
        {
            var dlqPayload = new
            {
                OriginalTopic = result.Topic,
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
                new KeyValuePair<string, object?>("event_type", result.Topic));

            Logger.LogCritical(
                "Poison pill from {Topic} partition {Partition} offset {Offset} sent to DLQ {DlqTopic} after {Attempts} retries. Error: {Error}",
                result.Topic, result.Partition.Value, result.Offset.Value, DlqTopic, attempts, ex.Message);
        }
        catch (Exception dlqEx)
        {
            Logger.LogCritical(dlqEx,
                "CRITICAL: Failed to send poison pill from {Topic} to DLQ {DlqTopic}. Manual intervention required.",
                result.Topic, DlqTopic);
        }
    }

    private static string OffsetKey(ConsumeResult<string, string?> result)
        => $"{result.Partition.Value}:{result.Offset.Value}";

    /// <summary>
    /// Implement message handling for the given topic.
    /// <paramref name="payload"/> is null for tombstone messages on compacted topics.
    /// <paramref name="messageKey"/> is the Kafka message key (e.g. entity ID for compacted topics).
    /// <paramref name="offset"/> is the Kafka partition offset — use for out-of-order protection.
    /// </summary>
    protected abstract Task ProcessAsync(
        string topic, string? payload, string messageKey, long offset,
        IServiceProvider services, CancellationToken ct);

    /// <summary>
    /// Helper for subclasses that need an explicit DB transaction around message processing.
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

    private void ValidateVersion(Headers headers, string topic)
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
                    "Received message on {Topic} with schema version '{Version}' — " +
                    "this consumer supports [{Supported}]. " +
                    "The producer may have a newer schema. Proceeding with best-effort deserialization.",
                    topic, version, string.Join(", ", SupportedVersions));
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
