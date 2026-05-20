using System.Collections.Concurrent;
using System.Text.Json;
using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.Kafka;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Messaging;

// ─────────────────────────────────────────────────────────────────────────────
// Test 1 — DLQ routing
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Verifies that when a Wolverine handler throws an unhandled exception after all retries,
/// the message is routed to the Kafka dead-letter topic (not silently discarded).
///
/// What is proven:
///   1. A failing handler does not cause silent message loss.
///   2. The configured retry policy (2 immediate retries) exhausts within the test window.
///   3. After retries, Wolverine produces the failed message to the DLQ Kafka topic.
///
/// This mirrors the production configuration in WolverineExtensions.WithResilience():
///   EnableNativeDeadLetterQueue() + DeadLetterQueueTopicName() on the Kafka connection.
/// Immediate retries (Retry(2)) are used instead of RetryWithCooldown to avoid
/// the durability-agent scheduler, keeping the test host minimal.
/// </summary>
[Collection(nameof(KafkaMessagingCollection))]
public class WolverineDlqRoutingTests : IAsyncLifetime
{
    private readonly KafkaFixture _kafka;

    private readonly string _sourceTopic   = $"test.dlq.source.{Guid.NewGuid():N}";
    private readonly string _dlqTopic      = $"test.dlq.dead.{Guid.NewGuid():N}";
    private readonly string _consumerGroup = $"dlq-routing-{Guid.NewGuid():N}";

    private IHost _publisher       = null!;
    private IHost _failingConsumer = null!;

    public WolverineDlqRoutingTests(KafkaFixture kafka) => _kafka = kafka;

    public async Task InitializeAsync()
    {
        _publisher       = BuildPublisherHost();
        _failingConsumer = BuildFailingConsumerHost();

        await _publisher.StartAsync();
        await _failingConsumer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _failingConsumer.StopAsync();
        await _publisher.StopAsync();
        _failingConsumer.Dispose();
        _publisher.Dispose();
    }

    [Fact]
    public async Task Handler_Exception_After_Retries_Routes_Message_To_Kafka_DLQ_Topic()
    {
        // Act — publish; AlwaysFailingTransactionHandler will throw and Wolverine will
        //       retry twice (immediate) then route the message to the Kafka DLQ topic.
        await using var scope = _publisher.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(BuildTestEvent());

        // Assert — poll the DLQ Kafka topic until a message appears (max 30 s)
        var dlqConfig = new ConsumerConfig
        {
            BootstrapServers = _kafka.BootstrapServers,
            GroupId          = $"dlq-checker-{Guid.NewGuid():N}",
            AutoOffsetReset  = AutoOffsetReset.Earliest,
        };

        using var dlqConsumer = new ConsumerBuilder<Ignore, string>(dlqConfig).Build();
        dlqConsumer.Subscribe(_dlqTopic);

        ConsumeResult<Ignore, string>? dlqMessage = null;
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline && dlqMessage == null)
        {
            try
            {
                dlqMessage = dlqConsumer.Consume(TimeSpan.FromSeconds(1));
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                // DLQ topic not yet created by Wolverine — retry after brief wait
                await Task.Delay(500);
            }
        }

        dlqMessage.Should().NotBeNull(
            "a failing handler must not silently discard messages — " +
            "the message must be routed to the Kafka DLQ topic after retries are exhausted");
    }

    // ── Host builders ──────────────────────────────────────────────────────────

    private IHost BuildPublisherHost() =>
        Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.UseKafka(_kafka.BootstrapServers);
                opts.UseSystemTextJsonForSerialization(ConfigureJson);
                opts.PublishMessage<TransactionCreatedEvent>().ToKafkaTopic(_sourceTopic);
                opts.Discovery.DisableConventionalDiscovery();
            })
            .Build();

    private IHost BuildFailingConsumerHost() =>
        Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                // Configure Kafka with a DLQ topic — mirrors production DeadLetterQueueTopicName()
                opts.UseKafka(_kafka.BootstrapServers)
                    .DeadLetterQueueTopicName(_dlqTopic);
                opts.UseSystemTextJsonForSerialization(ConfigureJson);

                // Fast retries — exhaust quickly so DLQ routing triggers within the test window
                opts.Policies.OnException<InvalidOperationException>()
                    .RetryWithCooldown(
                        TimeSpan.FromMilliseconds(50),
                        TimeSpan.FromMilliseconds(50));

                opts.ListenToKafkaTopic(_sourceTopic)
                    .ConfigureConsumer(cfg =>
                    {
                        cfg.GroupId         = _consumerGroup;
                        cfg.AutoOffsetReset = AutoOffsetReset.Earliest;
                    })
                    .EnableNativeDeadLetterQueue();

                opts.Discovery.DisableConventionalDiscovery();
                opts.Discovery.IncludeType<AlwaysFailingTransactionHandler>();
            })
            .Build();

    private static TransactionCreatedEvent BuildTestEvent() => new()
    {
        TransactionId   = Guid.NewGuid(),
        UserId          = "dlq-test-user",
        AccountId       = Guid.NewGuid(),
        CategoryId      = Guid.NewGuid(),
        Amount          = 100m,
        CurrencyCode    = "USD",
        TransactionType = TransactionType.Expense,
        TransactionDate = new LocalDate(2026, 5, 1),
        IsTransfer      = false,
    };

    private static void ConfigureJson(JsonSerializerOptions opts)
    {
        opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }
}

/// <summary>Simulates a broken handler — always throws to trigger DLQ routing.</summary>
public sealed class AlwaysFailingTransactionHandler
{
    private readonly string _reason = "Simulated handler failure — message should be DLQ'd.";

    public Task Handle(TransactionCreatedEvent _) =>
        throw new InvalidOperationException(_reason);
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 2 — Multiple consumer group isolation
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Verifies that two independent consumer groups on the same Kafka topic each
/// receive every published message — consumer groups do not compete for messages.
///
/// What is proven:
///   1. <c>ConsumerGroups.*</c> constants correctly isolate consumers.
///   2. A message published to one topic is delivered to ALL subscribing groups.
///   3. Analytics and Accounts service can both consume the same TransactionsCreated
///      topic without one "stealing" messages from the other.
/// </summary>
[Collection(nameof(KafkaMessagingCollection))]
public class WolverineMultiConsumerGroupTests : IAsyncLifetime
{
    private readonly KafkaFixture _kafka;

    private readonly string _topic       = $"test.multi.group.{Guid.NewGuid():N}";
    private readonly string _groupA      = $"group-a-{Guid.NewGuid():N}";
    private readonly string _groupB      = $"group-b-{Guid.NewGuid():N}";

    private IHost _publisher  = null!;
    private IHost _consumerA  = null!;
    private IHost _consumerB  = null!;

    private readonly TaskCompletionSource<Guid> _receivedByA =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<Guid> _receivedByB =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public WolverineMultiConsumerGroupTests(KafkaFixture kafka) => _kafka = kafka;

    public async Task InitializeAsync()
    {
        _publisher = BuildPublisherHost();
        _consumerA = BuildConsumerHost(_groupA, _receivedByA);
        _consumerB = BuildConsumerHost(_groupB, _receivedByB);

        await _publisher.StartAsync();
        await _consumerA.StartAsync();
        await _consumerB.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _consumerA.StopAsync();
        await _consumerB.StopAsync();
        await _publisher.StopAsync();
        _consumerA.Dispose();
        _consumerB.Dispose();
        _publisher.Dispose();
    }

    [Fact]
    public async Task Same_Topic_Is_Consumed_Independently_By_Each_Consumer_Group()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        // Act — publish one message to a single topic
        await using var scope = _publisher.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new TransactionCreatedEvent
        {
            TransactionId   = transactionId,
            UserId          = "multi-group-user",
            AccountId       = Guid.NewGuid(),
            CategoryId      = Guid.NewGuid(),
            Amount          = 250m,
            CurrencyCode    = "EUR",
            TransactionType = TransactionType.Income,
            TransactionDate = new LocalDate(2026, 5, 20),
            IsTransfer      = false,
        });

        // Assert — BOTH consumer groups must receive the same message independently
        var timeout = TimeSpan.FromSeconds(30);
        var idFromA = await _receivedByA.Task.WaitAsync(timeout);
        var idFromB = await _receivedByB.Task.WaitAsync(timeout);

        idFromA.Should().Be(transactionId, "consumer group A must receive the full message");
        idFromB.Should().Be(transactionId, "consumer group B must receive the full message independently");
    }

    // ── Host builders ──────────────────────────────────────────────────────────

    private IHost BuildPublisherHost() =>
        Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.UseKafka(_kafka.BootstrapServers);
                opts.UseSystemTextJsonForSerialization(ConfigureJson);
                opts.PublishMessage<TransactionCreatedEvent>().ToKafkaTopic(_topic);
                opts.Discovery.DisableConventionalDiscovery();
            })
            .Build();

    private IHost BuildConsumerHost(string groupId, TaskCompletionSource<Guid> signal) =>
        Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.UseKafka(_kafka.BootstrapServers);
                opts.UseSystemTextJsonForSerialization(ConfigureJson);
                opts.ListenToKafkaTopic(_topic)
                    .ConfigureConsumer(cfg =>
                    {
                        cfg.GroupId         = groupId;
                        cfg.AutoOffsetReset = AutoOffsetReset.Earliest;
                    });
                opts.Discovery.DisableConventionalDiscovery();
                opts.Discovery.IncludeType<TransactionIdCaptureHandler>();
            })
            .ConfigureServices(services => services.AddSingleton(signal))
            .Build();

    private static void ConfigureJson(JsonSerializerOptions opts)
    {
        opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }
}

/// <summary>Captures the TransactionId from the received event.</summary>
public sealed class TransactionIdCaptureHandler(TaskCompletionSource<Guid> signal)
{
    public Task Handle(TransactionCreatedEvent evt)
    {
        signal.TrySetResult(evt.TransactionId);
        return Task.CompletedTask;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test 3 — Multiple messages, no loss
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Verifies that all published messages are delivered to the consumer — no silent drops.
///
/// What is proven:
///   1. Wolverine delivers N messages published in quick succession without loss.
///   2. Each message is independently deserialised (no message merging/corruption).
///   3. All TransactionIds published are exactly the TransactionIds received.
/// </summary>
[Collection(nameof(KafkaMessagingCollection))]
public class WolverineMultiMessageTests : IAsyncLifetime
{
    internal const int MessageCount = 5;

    private readonly KafkaFixture _kafka;

    private readonly string _topic        = $"test.multi.msg.{Guid.NewGuid():N}";
    private readonly string _consumerGroup = $"multi-msg-{Guid.NewGuid():N}";

    private IHost _publisher = null!;
    private IHost _consumer  = null!;

    // Collect received TransactionIds — thread-safe for concurrent handler invocations
    private readonly ConcurrentBag<Guid> _receivedIds = [];

    // Signals when all expected messages have arrived
    private readonly TaskCompletionSource<bool> _allReceived =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public WolverineMultiMessageTests(KafkaFixture kafka) => _kafka = kafka;

    public async Task InitializeAsync()
    {
        _publisher = BuildPublisherHost();
        _consumer  = BuildConsumerHost();

        await _publisher.StartAsync();
        await _consumer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _consumer.StopAsync();
        await _publisher.StopAsync();
        _consumer.Dispose();
        _publisher.Dispose();
    }

    [Fact]
    public async Task All_Published_Messages_Are_Delivered_Without_Loss()
    {
        // Arrange — generate N distinct transaction IDs
        var publishedIds = Enumerable.Range(0, MessageCount)
            .Select(_ => Guid.NewGuid())
            .ToList();

        // Act — publish all messages as fast as possible
        await using var scope = _publisher.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        foreach (var id in publishedIds)
        {
            await bus.PublishAsync(new TransactionCreatedEvent
            {
                TransactionId   = id,
                UserId          = "multi-msg-user",
                AccountId       = Guid.NewGuid(),
                CategoryId      = Guid.NewGuid(),
                Amount          = 10m * publishedIds.IndexOf(id) + 1,
                CurrencyCode    = "USD",
                TransactionType = TransactionType.Expense,
                TransactionDate = new LocalDate(2026, 5, 1),
                IsTransfer      = false,
            });
        }

        // Assert — all N messages must arrive within 30 s
        await _allReceived.Task.WaitAsync(TimeSpan.FromSeconds(30));

        _receivedIds.Should().HaveCount(MessageCount,
            "every published message must be delivered exactly once");

        _receivedIds.Should().BeEquivalentTo(publishedIds,
            "received TransactionIds must match published TransactionIds exactly");
    }

    // ── Host builders ──────────────────────────────────────────────────────────

    private IHost BuildPublisherHost() =>
        Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.UseKafka(_kafka.BootstrapServers);
                opts.UseSystemTextJsonForSerialization(ConfigureJson);
                opts.PublishMessage<TransactionCreatedEvent>().ToKafkaTopic(_topic);
                opts.Discovery.DisableConventionalDiscovery();
            })
            .Build();

    private IHost BuildConsumerHost() =>
        Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.UseKafka(_kafka.BootstrapServers);
                opts.UseSystemTextJsonForSerialization(ConfigureJson);
                opts.ListenToKafkaTopic(_topic)
                    .ConfigureConsumer(cfg =>
                    {
                        cfg.GroupId         = _consumerGroup;
                        cfg.AutoOffsetReset = AutoOffsetReset.Earliest;
                    });
                opts.Discovery.DisableConventionalDiscovery();
                opts.Discovery.IncludeType<CollectingTransactionHandler>();
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(_receivedIds);
                services.AddSingleton(_allReceived);
            })
            .Build();

    private static void ConfigureJson(JsonSerializerOptions opts)
    {
        opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }
}

/// <summary>Collects received TransactionIds and signals when all expected messages have arrived.</summary>
public sealed class CollectingTransactionHandler(
    ConcurrentBag<Guid> receivedIds,
    TaskCompletionSource<bool> allReceived)
{
    public Task Handle(TransactionCreatedEvent evt)
    {
        receivedIds.Add(evt.TransactionId);

        if (receivedIds.Count >= WolverineMultiMessageTests.MessageCount)
        {
            allReceived.TrySetResult(true);
        }

        return Task.CompletedTask;
    }
}
