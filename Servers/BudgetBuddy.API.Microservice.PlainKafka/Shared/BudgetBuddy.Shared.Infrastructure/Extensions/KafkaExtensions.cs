using BudgetBuddy.Shared.Infrastructure.Messaging;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class KafkaExtensions
{
    public static IServiceCollection AddKafkaMessaging(
        this IServiceCollection services,
        IConfiguration config)
    {
        var settings = config.GetSection("Kafka").Get<KafkaSettings>() ?? new KafkaSettings();
        services.AddSingleton(settings);
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        // Kafka broker connectivity health check — tagged "ready" and "live"
        services.AddHealthChecks()
            .AddCheck<KafkaHealthCheck>("kafka", tags: ["ready", "live"]);

        return services;
    }
}
