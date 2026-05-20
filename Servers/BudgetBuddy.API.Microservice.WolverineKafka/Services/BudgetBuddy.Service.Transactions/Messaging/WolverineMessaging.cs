using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using BudgetBuddy.Shared.Messages.Topics;
using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.Kafka;

namespace BudgetBuddy.Service.Transactions.Messaging;

internal static class WolverineMessaging
{
    internal static WolverineOptions AddTransactionsMessaging(this WolverineOptions opts, IConfiguration configuration)
    {
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        opts.UseKafka(kafkaBootstrap)
            .DeadLetterQueueTopicName(TopicNames.TransactionsDlq);

        opts.AddDefaultRetryPolicy();

        // Outbound: transaction CRUD + saga compensation events
        opts.PublishMessage<TransactionCreatedEvent>().ToKafkaTopic(TopicNames.TransactionsCreated);
        opts.PublishMessage<TransactionUpdatedEvent>().ToKafkaTopic(TopicNames.TransactionsUpdated);
        opts.PublishMessage<TransactionDeletedEvent>().ToKafkaTopic(TopicNames.TransactionsDeleted);
        opts.PublishMessage<TransactionReversedEvent>().ToKafkaTopic(TopicNames.TransactionsReversed);

        // Inbound: saga — balance reservation result from Accounts
        opts.ListenToKafkaTopic(TopicNames.AccountsBalanceReserved)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.TransactionsSaga)
            .WithResilience();

        // Inbound: category snapshot maintenance
        opts.ListenToKafkaTopic(TopicNames.CategoryChanged)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.TransactionsReferenceData)
            .WithResilience();

        // Inbound: account snapshot handlers
        opts.ListenToKafkaTopic(TopicNames.AccountsCreated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.TransactionsAccounts)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.AccountsUpdated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.TransactionsAccounts)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.AccountsDeleted)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.TransactionsAccounts)
            .WithResilience();

        opts.WithEfCoreOutbox(configuration.GetConnectionString("DefaultConnection")!);

        return opts;
    }
}
