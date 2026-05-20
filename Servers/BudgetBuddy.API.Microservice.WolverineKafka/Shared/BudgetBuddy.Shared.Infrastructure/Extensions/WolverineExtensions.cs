using System.Reflection;
using System.Text.Json;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Kafka;
using Wolverine.Postgresql;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class WolverineExtensions
{
    /// <summary>
    /// Shared JSON options used by Wolverine for both publishing and ReceiveRawJson deserialization.
    /// CamelCase naming policy matches Wolverine's default; NodaTime converters ensure
    /// LocalDate is serialized as "2026-04-25" instead of a complex calendar object.
    /// </summary>
    /// <summary>
    /// JSON options for ReceiveRawJson — passed explicitly to each listener call.
    /// CamelCase matches Wolverine's default publish format; NodaTime converters
    /// ensure LocalDate deserializes as "2026-04-25" instead of a complex calendar object.
    /// </summary>
    public static readonly JsonSerializerOptions KafkaJsonOptions = BuildKafkaJsonOptions();

    private static JsonSerializerOptions BuildKafkaJsonOptions()
    {
        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        opts.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        return opts;
    }

    private static void ConfigureKafkaJsonOptions(JsonSerializerOptions opts)
    {
        opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    /// <summary>
    /// Registers Wolverine on an IHostApplicationBuilder (WebApplicationBuilder).
    /// Use this overload in Program.cs: builder.AddWolverine(...).
    /// Avoids IHostBuilder code path in WebApplicationFactory (fixes ObjectDisposedException in integration tests).
    /// </summary>
    public static IHostApplicationBuilder AddWolverine(
        this IHostApplicationBuilder builder,
        Assembly applicationAssembly,
        Action<WolverineOptions>? configure = null)
    {
        builder.Services.AddSingleton<SchemaVersionRegistry>();
        builder.Services.AddTransient<SchemaVersionMiddleware>();

        builder.UseWolverine(opts =>
        {
            opts.ApplicationAssembly = applicationAssembly;
            opts.Policies.Add<SchemaVersionPolicy>();
            opts.UseSystemTextJsonForSerialization(ConfigureKafkaJsonOptions);
            configure?.Invoke(opts);
        });

        return builder;
    }

    /// <summary>
    /// Configures the Kafka transport on existing WolverineOptions.
    /// Call this from UseWolverine() inside each service's AddWolverine() callback.
    /// </summary>
    public static WolverineOptions UseKafkaTransport(
        this WolverineOptions opts,
        string bootstrapServers)
    {
        opts.UseKafka(bootstrapServers);
        return opts;
    }

    /// <summary>
    /// Adds EF Core transactional outbox support with PostgreSQL durable message persistence.
    /// Each DbContext must call modelBuilder.MapWolverineEnvelopeStorage(schema) in OnModelCreating.
    /// </summary>
    public static WolverineOptions WithEfCoreOutbox(this WolverineOptions opts, string connectionString)
    {
        opts.PersistMessagesWithPostgresql(connectionString, "wolverine");
        opts.UseEntityFrameworkCoreTransactions();
        return opts;
    }

    /// <summary>
    /// Applies a global exponential-backoff retry policy for all message failures.
    /// 5 attempts: 200 ms → 400 ms → 800 ms → 1 600 ms → 3 200 ms, then move to dead-letter storage.
    /// </summary>
    public static WolverineOptions AddDefaultRetryPolicy(this WolverineOptions opts)
    {
        opts.Policies.OnException<Exception>()
            .RetryWithCooldown(
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(400),
                TimeSpan.FromMilliseconds(800),
                TimeSpan.FromMilliseconds(1_600),
                TimeSpan.FromMilliseconds(3_200));

        return opts;
    }

    /// <summary>
    /// Enables native Kafka DLQ routing and a circuit breaker on this listener.
    /// Apply after .ConfigureConsumer(...) on every ListenToKafkaTopic() call.
    ///
    /// Circuit breaker: MinimumThreshold=5, FailurePercentage=20 %, PauseTime=1 min, TrackingPeriod=10 min.
    /// Failed messages are produced to the service's DLQ topic (set via UseKafka().DeadLetterQueueTopicName()).
    /// </summary>
    public static KafkaListenerConfiguration WithResilience(this KafkaListenerConfiguration config)
    {
        return config
            .EnableNativeDeadLetterQueue()
            .CircuitBreaker(cb =>
            {
                cb.MinimumThreshold           = 5;
                cb.FailurePercentageThreshold = 20;
                cb.PauseTime                  = TimeSpan.FromMinutes(1);
                cb.TrackingPeriod             = TimeSpan.FromMinutes(10);
            });
    }
}
