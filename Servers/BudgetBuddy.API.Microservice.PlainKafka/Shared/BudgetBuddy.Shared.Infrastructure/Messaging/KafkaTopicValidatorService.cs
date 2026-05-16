using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace BudgetBuddy.Shared.Infrastructure.Messaging;

/// <summary>
/// Validates that all required Kafka topics exist at application startup.
/// When <see cref="KafkaTopicValidatorOptions.CreateIfMissing"/> is <c>true</c> (recommended for
/// development/testing only) missing topics are created automatically with the configured defaults.
/// In production the service stops the application if any topics are missing to prevent silent
/// message loss — topics must be provisioned deliberately with correct partition/replica settings.
/// </summary>
public sealed class KafkaTopicValidatorService(
    KafkaSettings settings,
    KafkaTopicValidatorOptions options,
    ILogger<KafkaTopicValidatorService> logger,
    IHostApplicationLifetime lifetime) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.RequiredTopics.Length == 0)
        {
            return;
        }

        try
        {
            var adminConfig = new AdminClientConfig { BootstrapServers = settings.BootstrapServers };
            settings.ApplySecurity(adminConfig);
            using var adminClient = new AdminClientBuilder(adminConfig).Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(15));
            var existingTopics = metadata.Topics
                .Where(t => t.Error.Code == ErrorCode.NoError)
                .Select(t => t.Topic)
                .ToHashSet(StringComparer.Ordinal);

            var missingTopics = options.RequiredTopics
                .Where(t => !existingTopics.Contains(t))
                .ToList();

            if (missingTopics.Count == 0)
            {
                logger.LogInformation(
                    "Kafka topic validation passed: all {Count} required topics exist.",
                    options.RequiredTopics.Length);
                return;
            }

            if (options.CreateIfMissing)
            {
                // Auto-create is intentionally limited to dev/test environments.
                // Production topics must be created with deliberate partition/replica settings.
                var specs = missingTopics.Select(t => new TopicSpecification
                {
                    Name              = t,
                    NumPartitions     = options.DefaultNumPartitions,
                    ReplicationFactor = options.DefaultReplicationFactor,
                }).ToList();

                await adminClient.CreateTopicsAsync(specs);

                logger.LogInformation(
                    "Kafka auto-created {Count} missing topics: {Topics}",
                    missingTopics.Count, missingTopics);
            }
            else
            {
                logger.LogCritical(
                    "Kafka topic validation failed — missing topics: {MissingTopics}. Application will not start.",
                    missingTopics);
                lifetime.StopApplication();
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "Kafka topic validation could not reach broker at {BootstrapServers}. Application will not start.",
                settings.BootstrapServers);
            lifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Options for <see cref="KafkaTopicValidatorService"/>.</summary>
public sealed class KafkaTopicValidatorOptions
{
    public string[] RequiredTopics        { get; init; } = [];

    /// <summary>
    /// When <c>true</c>, missing topics are created automatically at startup.
    /// Should only be enabled for Development / Testing environments.
    /// </summary>
    public bool CreateIfMissing           { get; init; } = false;

    /// <summary>Default partition count used when auto-creating topics.</summary>
    public int  DefaultNumPartitions      { get; init; } = 3;

    /// <summary>Default replication factor used when auto-creating topics.</summary>
    public short DefaultReplicationFactor { get; init; } = 1;
}

public static class KafkaTopicValidationExtensions
{
    /// <summary>
    /// Registers a startup validator that ensures all <paramref name="requiredTopics"/> exist in Kafka.
    /// Call this from each service's Program.cs or RegisterServices(), passing the topics it consumes/produces.
    /// </summary>
    /// <summary>
    /// Validates required topics exist at startup; stops the application if any are missing.
    /// </summary>
    public static IServiceCollection AddKafkaTopicValidation(
        this IServiceCollection services,
        params string[] requiredTopics)
        => services.AddKafkaTopicValidation(createIfMissing: false, requiredTopics);

    /// <summary>
    /// Validates required topics exist at startup.
    /// When <paramref name="createIfMissing"/> is <c>true</c>, missing topics are created
    /// automatically — intended for Development / Testing environments only.
    /// </summary>
    public static IServiceCollection AddKafkaTopicValidation(
        this IServiceCollection services,
        bool createIfMissing,
        params string[] requiredTopics)
    {
        services.AddSingleton(new KafkaTopicValidatorOptions
        {
            RequiredTopics  = requiredTopics,
            CreateIfMissing = createIfMissing,
        });
        services.AddHostedService<KafkaTopicValidatorService>();
        return services;
    }
}
