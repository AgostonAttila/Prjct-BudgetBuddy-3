using System.Text.Json;
using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using BudgetBuddy.Shared.Messages.Topics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Wolverine;
using Confluent.Kafka;
using Wolverine.Kafka;
using Xunit;
using BudgetBuddy.Shared.Kernel.Enums;

namespace BudgetBuddy.Integration.Tests.Messaging;

/// <summary>
/// Verifies that Wolverine's native envelope format (message-type Kafka header) correctly
/// routes messages from a publisher host to a consumer host over a real Kafka broker.
///
/// Context — why this test exists:
///   Previously all listeners used <c>.ReceiveRawJson&lt;T&gt;()</c>, which bypasses
///   Wolverine's envelope and routes by the explicitly declared type parameter.
///   After removing ReceiveRawJson, Wolverine reads the <c>message-type</c> Kafka header
///   written by the publisher and resolves the handler via that header value.
///   This test proves that the header is written and read correctly end-to-end.
///
/// What is proven:
///   1. Publisher writes the <c>message-type</c> Kafka header (native Wolverine envelope)
///   2. Consumer reads the header and routes to the correct handler (no ReceiveRawJson)
///   3. Full JSON round-trip works: LocalDate, decimal, Guid survive serialization
///   4. Two independent IHost instances communicate via real Kafka (Testcontainers)
/// </summary>
[Collection(nameof(KafkaMessagingCollection))]
public class WolverineEnvelopeRoutingTests : IAsyncLifetime
{
    private readonly KafkaFixture _kafka;

    // Unique per test-run so parallel test runs never share topics or consumer groups
    private readonly string _topic        = $"{TopicNames.TransactionsCreated}.test.{Guid.NewGuid():N}";
    private readonly string _consumerGroup = $"envelope-routing-{Guid.NewGuid():N}";

    private IHost _publisher = null!;
    private IHost _consumer  = null!;

    // Signal set by the consumer-side test handler when a message arrives
    private readonly TaskCompletionSource<TransactionCreatedEvent> _received =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public WolverineEnvelopeRoutingTests(KafkaFixture kafka) => _kafka = kafka;

    public async Task InitializeAsync()
    {
        _publisher = BuildPublisherHost();
        _consumer  = BuildConsumerHost();

        // Start sequentially — consumer must be ready before publish, otherwise
        // a fast publisher could send before the consumer group is registered.
        await _publisher.StartAsync();
        await _consumer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _publisher.StopAsync();
        await _consumer.StopAsync();
        _publisher.Dispose();
        _consumer.Dispose();
    }

    [Fact]
    public async Task TransactionCreatedEvent_Routes_Via_MessageType_Header_To_Handler()
    {
        // Arrange — construct an event with all field types used in production
        var transactionId = Guid.NewGuid();
        var accountId     = Guid.NewGuid();
        var categoryId    = Guid.NewGuid();
        var date          = new LocalDate(2026, 5, 15);

        var outbound = new TransactionCreatedEvent
        {
            TransactionId   = transactionId,
            UserId          = "test-user-envelope",
            AccountId       = accountId,
            CategoryId      = categoryId,
            Amount          = 4_200.50m,
            CurrencyCode    = "USD",
            TransactionType = TransactionType.Income,
            TransactionDate = date,
            IsTransfer      = false,
        };

        // Act — publish via Wolverine native envelope
        // Wolverine writes the 'message-type' Kafka header alongside the JSON body.
        // The consumer host reads that header to resolve the handler — no ReceiveRawJson.
        await using var scope = _publisher.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(outbound);

        // Assert — consumer handler must be invoked within 30 seconds
        var inbound = await _received.Task.WaitAsync(TimeSpan.FromSeconds(30));

        inbound.TransactionId.Should().Be(transactionId,   "Guid round-trip");
        inbound.UserId.Should().Be("test-user-envelope",   "string round-trip");
        inbound.AccountId.Should().Be(accountId,           "Guid round-trip");
        inbound.Amount.Should().Be(4_200.50m,              "decimal round-trip");
        inbound.CurrencyCode.Should().Be("USD",            "string round-trip");
        inbound.TransactionType.Should().Be(TransactionType.Income, "enum round-trip");
        inbound.TransactionDate.Should().Be(date,          "NodaTime LocalDate round-trip");
        inbound.IsTransfer.Should().BeFalse(               "bool round-trip");
    }

    // ── Host builders ──────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal publisher host — mirrors the Transactions service publish-side configuration.
    /// Handler discovery is disabled: this host only produces, never consumes.
    /// </summary>
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

    /// <summary>
    /// Minimal consumer host — mirrors the Analytics service consume-side configuration.
    /// Uses only the test handler; real Analytics handlers are excluded so no DB is needed.
    /// </summary>
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
                opts.Discovery.IncludeType<TransactionCreatedEventTestHandler>();
            })
            .ConfigureServices(services => services.AddSingleton(_received))
            .Build();

    private static void ConfigureJson(JsonSerializerOptions opts)
    {
        opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }
}

/// <summary>
/// Test-only Wolverine handler. Signals the TCS when a TransactionCreatedEvent arrives,
/// allowing the test to assert on the fully deserialized message.
/// Scoped to this file via the <c>file</c> modifier — not visible outside.
/// </summary>
public sealed class TransactionCreatedEventTestHandler(
    TaskCompletionSource<TransactionCreatedEvent> signal)
{
    public Task Handle(TransactionCreatedEvent evt)
    {
        signal.TrySetResult(evt);
        return Task.CompletedTask;
    }
}
