using System.Text.Json;
using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Integration;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Messaging;

[Collection(nameof(KafkaCollection))]
public class KafkaEventPublisherTests(KafkaFixture kafka)
{
    [Fact]
    public async Task PublishAsync_ShouldDeliver_MessageToKafka()
    {
        // Each test uses a unique topic to avoid cross-test stale-message contamination.
        // The topic is pre-created so the consumer doesn't hit "Unknown topic" before auto-create.
        var topic = $"test.events.{Guid.NewGuid():N}";
        await kafka.CreateTopicAsync(topic);

        var settings = new KafkaSettings { BootstrapServers = kafka.BootstrapServers };
        using var publisher = new KafkaEventPublisher(settings, NullLogger<KafkaEventPublisher>.Instance);

        using var consumer = kafka.CreateConsumer($"grp-{Guid.NewGuid():N}");
        consumer.Subscribe(topic);

        var testEvent = new TestIntegrationEvent
        {
            MessageId     = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Payload       = "hello from integration test",
        };

        await publisher.PublishAsync(testEvent, topic);

        var result = consumer.Consume(TimeSpan.FromSeconds(10));
        result.Should().NotBeNull();

        var deserialized = JsonSerializer.Deserialize<TestIntegrationEvent>(result.Message.Value);
        deserialized!.MessageId.Should().Be(testEvent.MessageId);
        deserialized.Payload.Should().Be(testEvent.Payload);
    }

    [Fact]
    public async Task PublishAsync_ShouldSet_CorrelationIdHeader()
    {
        var topic = $"test.events.{Guid.NewGuid():N}";
        await kafka.CreateTopicAsync(topic);

        var settings = new KafkaSettings { BootstrapServers = kafka.BootstrapServers };
        using var publisher = new KafkaEventPublisher(settings, NullLogger<KafkaEventPublisher>.Instance);

        using var consumer = kafka.CreateConsumer($"grp-{Guid.NewGuid():N}");
        consumer.Subscribe(topic);

        var correlationId = Guid.NewGuid();
        var testEvent = new TestIntegrationEvent
        {
            MessageId     = Guid.NewGuid(),
            CorrelationId = correlationId,
            Payload       = "header test",
        };

        await publisher.PublishAsync(testEvent, topic);

        var result = consumer.Consume(TimeSpan.FromSeconds(10));
        var header = result.Message.Headers.FirstOrDefault(h => h.Key == "correlation-id");
        header.Should().NotBeNull();
        System.Text.Encoding.UTF8.GetString(header!.GetValueBytes())
            .Should().Be(correlationId.ToString());
    }

    [Fact]
    public async Task PublishStateAsync_ShouldDeliver_StateMessage()
    {
        var topic = $"test.state.{Guid.NewGuid():N}";
        await kafka.CreateTopicAsync(topic);

        var settings = new KafkaSettings { BootstrapServers = kafka.BootstrapServers };
        using var publisher = new KafkaEventPublisher(settings, NullLogger<KafkaEventPublisher>.Instance);

        using var consumer = kafka.CreateConsumer($"grp-{Guid.NewGuid():N}");
        consumer.Subscribe(topic);

        var state = new { Id = Guid.NewGuid(), Name = "test-state", Value = 42 };

        await publisher.PublishStateAsync(topic, state.Id.ToString(), state);

        var result = consumer.Consume(TimeSpan.FromSeconds(10));
        result.Message.Key.Should().Be(state.Id.ToString());
        result.Message.Value.Should().Contain("test-state");
    }

    private record TestIntegrationEvent : IIntegrationEvent
    {
        public Guid MessageId     { get; init; }
        public Guid CorrelationId { get; init; }
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
        public string Version     { get; init; } = "1.0";
        public string Payload     { get; init; } = string.Empty;
    }
}
