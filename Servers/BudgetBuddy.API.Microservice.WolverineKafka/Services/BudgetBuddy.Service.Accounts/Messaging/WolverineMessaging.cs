using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using BudgetBuddy.Shared.Messages.Topics;
using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.Kafka;

namespace BudgetBuddy.Service.Accounts.Messaging;

internal static class WolverineMessaging
{
    internal static WolverineOptions AddAccountsMessaging(this WolverineOptions opts, IConfiguration configuration)
    {
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        opts.UseKafka(kafkaBootstrap)
            .DeadLetterQueueTopicName(TopicNames.AccountsDlq);

        opts.AddDefaultRetryPolicy();

        // Outbound: account CRUD events
        opts.PublishMessage<AccountCreatedEvent>().ToKafkaTopic(TopicNames.AccountsCreated);
        opts.PublishMessage<AccountUpdatedEvent>().ToKafkaTopic(TopicNames.AccountsUpdated);
        opts.PublishMessage<AccountDeletedEvent>().ToKafkaTopic(TopicNames.AccountsDeleted);

        // Inbound: transaction events → update AccountTransactionTotals read model
        opts.ListenToKafkaTopic(TopicNames.TransactionsCreated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AccountsTransactions)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.TransactionsUpdated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AccountsTransactions)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.TransactionsDeleted)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AccountsTransactions)
            .WithResilience();

        opts.WithEfCoreOutbox(configuration.GetConnectionString("DefaultConnection")!);

        return opts;
    }
}
