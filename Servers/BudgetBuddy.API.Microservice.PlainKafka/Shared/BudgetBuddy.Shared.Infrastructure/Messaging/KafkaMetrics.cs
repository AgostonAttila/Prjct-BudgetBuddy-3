using System.Diagnostics.Metrics;

namespace BudgetBuddy.Shared.Infrastructure.Messaging;

public static class KafkaMetrics
{
    private static readonly Meter Meter = new("BudgetBuddy.Kafka");

    public static readonly Counter<long> MessagesPublished =
        Meter.CreateCounter<long>("kafka_messages_published_total",
            unit: "{messages}",
            description: "Total Kafka messages published");

    public static readonly Counter<long> MessagesConsumed =
        Meter.CreateCounter<long>("kafka_messages_consumed_total",
            unit: "{messages}",
            description: "Total Kafka messages consumed");

    public static readonly Counter<long> MessagesFailed =
        Meter.CreateCounter<long>("kafka_messages_failed_total",
            unit: "{messages}",
            description: "Total Kafka messages that failed processing");

    public static readonly Histogram<double> ProcessingDuration =
        Meter.CreateHistogram<double>("kafka_message_processing_duration_seconds",
            unit: "s",
            description: "Time to process a Kafka message");

    public static readonly Counter<long> MessagesSentToDlq =
        Meter.CreateCounter<long>("kafka_messages_sent_to_dlq_total",
            unit: "{messages}",
            description: "Total Kafka messages sent to Dead Letter Queue after max retries");

    /// <summary>
    /// Per-partition consumer lag reported by the Kafka statistics callback.
    /// Updated every StatisticsIntervalMs (default 60 s).
    /// Item 18: Kafka Statistics → Prometheus producer/consumer lag
    /// </summary>
    public static readonly ObservableGauge<long> ConsumerLag =
        Meter.CreateObservableGauge<long>(
            "kafka_consumer_lag",
            observeValues: () => (ConsumerLagValues ?? []).Select(kv =>
                new Measurement<long>(kv.Value,
                    new KeyValuePair<string, object?>("partition", kv.Key))),
            unit: "{messages}",
            description: "Current consumer lag per topic/partition");

    // Stores the latest lag samples; updated from the statistics handler.
    // ConcurrentDictionary so producer and consumer threads don't race.
    internal static readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> ConsumerLagValues =
        new();

    /// <summary>
    /// Parses a Confluent.Kafka statistics JSON blob and records consumer lag
    /// per topic+partition into <see cref="ConsumerLagValues"/>.
    /// Call this from the consumer's SetStatisticsHandler.
    /// </summary>
    public static void RecordStatistics(string statisticsJson)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(statisticsJson);
            if (!doc.RootElement.TryGetProperty("topics", out var topics))
            {
                return;
            }

            foreach (var topic in topics.EnumerateObject())
            {
                if (!topic.Value.TryGetProperty("partitions", out var partitions))
                {
                    continue;
                }

                foreach (var partition in partitions.EnumerateObject())
                {
                    // -1 means the partition is not assigned; skip it
                    if (!partition.Value.TryGetProperty("consumer_lag", out var lagProp))
                    {
                        continue;
                    }

                    var lag = lagProp.GetInt64();
                    if (lag < 0)
                    {
                        continue;
                    }

                    var key = $"{topic.Name}:{partition.Name}";
                    ConsumerLagValues[key] = lag;
                }
            }
        }
        catch
        {
            // Ignore parse errors — statistics are best-effort
        }
    }
}
