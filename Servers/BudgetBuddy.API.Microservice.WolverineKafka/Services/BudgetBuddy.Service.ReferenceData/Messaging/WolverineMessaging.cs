using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Messages.Events.ReferenceData;
using BudgetBuddy.Shared.Messages.Topics;
using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.Kafka;

namespace BudgetBuddy.Service.ReferenceData.Messaging;

internal static class WolverineMessaging
{
    internal static WolverineOptions AddReferenceDataMessaging(this WolverineOptions opts, IConfiguration configuration)
    {
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        opts.UseKafka(kafkaBootstrap)
            .DeadLetterQueueTopicName(TopicNames.ReferenceDataDlq);

        opts.AddDefaultRetryPolicy();

        // Outbound: category change events (atomic with DB write via Wolverine outbox)
        opts.PublishMessage<CategoryChangedEvent>().ToKafkaTopic(TopicNames.CategoryChanged);

        // Inbound: account snapshot handlers
        opts.ListenToKafkaTopic(TopicNames.AccountsCreated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.ReferenceDataAccounts)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.AccountsUpdated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.ReferenceDataAccounts)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.AccountsDeleted)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.ReferenceDataAccounts)
            .WithResilience();

        opts.WithEfCoreOutbox(configuration.GetConnectionString("DefaultConnection")!);

        return opts;
    }
}
