using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Messages.Topics;
using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.Kafka;

namespace BudgetBuddy.Service.Notifications.Messaging;

internal static class WolverineMessaging
{
    internal static WolverineOptions AddNotificationsMessaging(this WolverineOptions opts, IConfiguration configuration)
    {
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        opts.UseKafka(kafkaBootstrap)
            .DeadLetterQueueTopicName(TopicNames.NotificationsDlq);

        opts.AddDefaultRetryPolicy();

        opts.ListenToKafkaTopic(TopicNames.BudgetAlertTriggered)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.NotificationsBudgets)
            .WithResilience();

        // No WithEfCoreOutbox() — notifications has no database

        return opts;
    }
}
