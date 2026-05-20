using Testcontainers.Kafka;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

/// <summary>
/// Shared Testcontainers Kafka fixture. One broker instance is started per collection
/// and reused across all tests in that collection to minimise startup overhead.
/// </summary>
public sealed class KafkaFixture : IAsyncLifetime
{
    private readonly KafkaContainer _kafka = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.9.0")
        .Build();

    /// <summary>Kafka bootstrap address for Wolverine <c>opts.UseKafka()</c> calls.</summary>
    public string BootstrapServers => _kafka.GetBootstrapAddress();

    public Task InitializeAsync() => _kafka.StartAsync();

    public Task DisposeAsync() => _kafka.DisposeAsync().AsTask();
}
