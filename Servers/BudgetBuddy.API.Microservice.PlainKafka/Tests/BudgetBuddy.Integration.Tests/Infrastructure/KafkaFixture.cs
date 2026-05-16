using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Testcontainers.Kafka;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

/// <summary>
/// xUnit fixture: egyetlen Kafka konténert indít a teljes test futáshoz.
/// Használat: [Collection(nameof(KafkaCollection))]
/// </summary>
public sealed class KafkaFixture : IAsyncLifetime
{
    private readonly KafkaContainer _kafka = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.9.0")
        .Build();

    public string BootstrapServers => _kafka.GetBootstrapAddress();

    public async Task InitializeAsync() => await _kafka.StartAsync();

    public async Task DisposeAsync() => await _kafka.DisposeAsync();

    /// <summary>
    /// Pre-creates a topic so consumers can subscribe without getting
    /// "Unknown topic or partition" before auto-create kicks in.
    /// </summary>
    public async Task CreateTopicAsync(string topicName, int numPartitions = 1, short replicationFactor = 1)
    {
        using var admin = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = BootstrapServers }).Build();

        await admin.CreateTopicsAsync([new TopicSpecification
        {
            Name              = topicName,
            NumPartitions     = numPartitions,
            ReplicationFactor = replicationFactor,
        }]);
    }

    public IProducer<string, string> CreateProducer()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All,
        };
        return new ProducerBuilder<string, string>(config).Build();
    }

    public IConsumer<string, string> CreateConsumer(string groupId)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = BootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };
        return new ConsumerBuilder<string, string>(config).Build();
    }
}

[CollectionDefinition(nameof(KafkaCollection))]
public class KafkaCollection : ICollectionFixture<KafkaFixture>;
