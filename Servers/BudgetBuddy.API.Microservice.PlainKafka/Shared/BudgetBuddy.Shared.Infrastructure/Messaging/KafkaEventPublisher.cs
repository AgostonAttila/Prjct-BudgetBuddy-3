using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BudgetBuddy.Shared.Messages.Integration;
using Confluent.Kafka;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace BudgetBuddy.Shared.Infrastructure.Messaging;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions { WriteIndented = false }
            .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

    private readonly IProducer<string, string?> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(KafkaSettings settings, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            Acks             = Acks.All,
            // linger.ms defaults to 0 (immediate send, no batching). This is intentional:
            // BudgetBuddy publishes individual domain events where low latency matters more
            // than throughput. For bulk scenarios (e.g. bulk import), override via KafkaSettings.
            EnableIdempotence     = true,
            // With EnableIdempotence=true, librdkafka guarantees per-partition ordering for
            // up to 5 in-flight requests (Confluent docs, finding #10). MaxInFlight=1 would be
            // stricter but halves throughput with no correctness benefit on idempotent producers.
            MaxInFlight           = 5,
            MessageSendMaxRetries = 10,
            RetryBackoffMs        = 100,
            RetryBackoffMaxMs     = 1000,
            CompressionType       = CompressionType.Snappy,
            StatisticsIntervalMs  = settings.StatisticsIntervalMs,
        };
        settings.ApplySecurity(config);
        _producer = new ProducerBuilder<string, string?>(config)
            // Item 18: log queue depth / in-flight count from producer stats
            .SetStatisticsHandler((_, json) =>
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var msgCount  = doc.RootElement.TryGetProperty("msg_cnt",  out var mc) ? mc.GetInt64() : 0;
                    var queueSize = doc.RootElement.TryGetProperty("msg_size", out var ms) ? ms.GetInt64() : 0;
                    logger.LogDebug("Kafka producer stats — queued msgs: {MsgCount}, queue bytes: {QueueSize}",
                        msgCount, queueSize);
                }
                catch { /* stats are best-effort */ }
            })
            .Build();
    }

    public Task PublishAsync<T>(T @event, string topic, CancellationToken ct = default)
        where T : IIntegrationEvent
        => PublishAsync(@event, topic, partitionKey: @event.CorrelationId.ToString(), ct);

    public async Task PublishAsync<T>(T @event, string topic, string partitionKey, CancellationToken ct = default)
        where T : IIntegrationEvent
    {
        var headers = BuildHeaders(typeof(T).Name, @event.Version, @event.CorrelationId);
        AddTraceHeaders(headers);

        var message = new Message<string, string?>
        {
            Key     = partitionKey,
            Value   = JsonSerializer.Serialize(@event, JsonOptions),
            Headers = headers,
        };

        var result = await _producer.ProduceAsync(topic, message, ct);

        _logger.LogInformation(
            "Published {EventType} to {Topic} partition {Partition} offset {Offset} key {Key}",
            typeof(T).Name, topic, result.Partition.Value, result.Offset.Value, partitionKey);

        KafkaMetrics.MessagesPublished.Add(1,
            new KeyValuePair<string, object?>("topic", topic),
            new KeyValuePair<string, object?>("event_type", typeof(T).Name));
    }

    public Task PublishAsync(IIntegrationEvent @event, Type eventType, string topic, CancellationToken ct = default)
        => PublishAsync(@event, eventType, topic, partitionKey: @event.CorrelationId.ToString(), ct);

    public async Task PublishAsync(IIntegrationEvent @event, Type eventType, string topic, string partitionKey, CancellationToken ct = default)
    {
        var headers = BuildHeaders(eventType.Name, @event.Version, @event.CorrelationId);
        AddTraceHeaders(headers);

        var message = new Message<string, string?>
        {
            Key     = partitionKey,
            Value   = JsonSerializer.Serialize(@event, eventType, JsonOptions),
            Headers = headers,
        };

        var result = await _producer.ProduceAsync(topic, message, ct);

        _logger.LogInformation(
            "Published {EventType} to {Topic} partition {Partition} offset {Offset} key {Key}",
            eventType.Name, topic, result.Partition.Value, result.Offset.Value, partitionKey);

        KafkaMetrics.MessagesPublished.Add(1,
            new KeyValuePair<string, object?>("topic", topic),
            new KeyValuePair<string, object?>("event_type", eventType.Name));
    }

    public async Task PublishStateAsync<T>(string topic, string key, T state, CancellationToken ct = default)
    {
        var message = new Message<string, string?>
        {
            Key   = key,
            Value = JsonSerializer.Serialize(state, JsonOptions),
        };
        var result = await _producer.ProduceAsync(topic, message, ct);

        _logger.LogDebug(
            "Published state to compacted topic {Topic} partition {Partition} offset {Offset} key {Key}",
            topic, result.Partition.Value, result.Offset.Value, key);

        KafkaMetrics.MessagesPublished.Add(1,
            new KeyValuePair<string, object?>("topic", topic),
            new KeyValuePair<string, object?>("event_type", typeof(T).Name));
    }

    public async Task PublishTombstoneAsync(string topic, string key, CancellationToken ct = default)
    {
        var message = new Message<string, string?> { Key = key, Value = null };
        var result = await _producer.ProduceAsync(topic, message, ct);
        _logger.LogInformation(
            "Published tombstone to {Topic} partition {Partition} offset {Offset} key {Key}",
            topic, result.Partition.Value, result.Offset.Value, key);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Headers BuildHeaders(string eventType, string version, Guid correlationId) =>
        new()
        {
            { "event-type",     Encoding.UTF8.GetBytes(eventType) },
            { "version",        Encoding.UTF8.GetBytes(version) },
            { "correlation-id", Encoding.UTF8.GetBytes(correlationId.ToString()) },
        };

    /// <summary>
    /// Injects W3C traceparent/tracestate from the current Activity so that
    /// distributed traces span across Kafka message boundaries (O-01).
    /// </summary>
    private static void AddTraceHeaders(Headers headers)
    {
        var activity = Activity.Current;
        if (activity?.Id is null)
        {
            return;
        }

        headers.Add("traceparent", Encoding.UTF8.GetBytes(activity.Id));

        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            headers.Add("tracestate", Encoding.UTF8.GetBytes(activity.TraceStateString));
        }
    }
}
