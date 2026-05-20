using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Messages.Events.Budgets;
using BudgetBuddy.Shared.Messages.Topics;
using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.Kafka;

namespace BudgetBuddy.Service.Budgets.Messaging;

internal static class WolverineMessaging
{
    internal static WolverineOptions AddBudgetsMessaging(this WolverineOptions opts, IConfiguration configuration)
    {
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        opts.UseKafka(kafkaBootstrap)
            .DeadLetterQueueTopicName(TopicNames.BudgetsDlq);

        opts.AddDefaultRetryPolicy();

        // Outbound: budget CRUD + alert events
        opts.PublishMessage<BudgetCreatedEvent>().ToKafkaTopic(TopicNames.BudgetsCreated);
        opts.PublishMessage<BudgetUpdatedEvent>().ToKafkaTopic(TopicNames.BudgetsUpdated);
        opts.PublishMessage<BudgetDeletedEvent>().ToKafkaTopic(TopicNames.BudgetsDeleted);
        opts.PublishMessage<BudgetAlertTriggeredEvent>().ToKafkaTopic(TopicNames.BudgetAlertTriggered);

        // Inbound: category snapshot maintenance
        opts.ListenToKafkaTopic(TopicNames.CategoryChanged)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.BudgetsReferenceData)
            .WithResilience();

        // Inbound: spending aggregate maintenance
        opts.ListenToKafkaTopic(TopicNames.TransactionsCreated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.BudgetsTransactions)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.TransactionsUpdated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.BudgetsTransactions)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.TransactionsDeleted)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.BudgetsTransactions)
            .WithResilience();

        opts.WithEfCoreOutbox(configuration.GetConnectionString("DefaultConnection")!);

        return opts;
    }
}
