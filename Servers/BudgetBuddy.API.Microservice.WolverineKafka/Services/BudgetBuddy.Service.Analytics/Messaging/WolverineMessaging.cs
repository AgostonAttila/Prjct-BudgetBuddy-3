using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Messages.Topics;
using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.Kafka;

namespace BudgetBuddy.Service.Analytics.Messaging;

internal static class WolverineMessaging
{
    internal static WolverineOptions AddAnalyticsMessaging(this WolverineOptions opts, IConfiguration configuration)
    {
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        opts.UseKafka(kafkaBootstrap)
            .DeadLetterQueueTopicName(TopicNames.AnalyticsDlq);

        opts.AddDefaultRetryPolicy();

        // Inbound: transactions read model
        opts.ListenToKafkaTopic(TopicNames.TransactionsCreated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsTransactions)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.TransactionsUpdated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsTransactions)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.TransactionsDeleted)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsTransactions)
            .WithResilience();

        // Inbound: budgets read model
        opts.ListenToKafkaTopic(TopicNames.BudgetsCreated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsBudgets)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.BudgetsUpdated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsBudgets)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.BudgetsDeleted)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsBudgets)
            .WithResilience();

        // Inbound: investments read model
        opts.ListenToKafkaTopic(TopicNames.InvestmentsCreated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsInvestments)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.InvestmentsUpdated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsInvestments)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.InvestmentsDeleted)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsInvestments)
            .WithResilience();

        // Inbound: category snapshot
        opts.ListenToKafkaTopic(TopicNames.CategoryChanged)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsReferenceData)
            .WithResilience();

        // Inbound: market price snapshots
        opts.ListenToKafkaTopic(TopicNames.MarketPriceUpdated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsMarketPrice)
            .WithResilience();

        // Inbound: account snapshot handlers
        opts.ListenToKafkaTopic(TopicNames.AccountsCreated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsAccounts)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.AccountsUpdated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsAccounts)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.AccountsDeleted)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.AnalyticsAccounts)
            .WithResilience();

        // No WithEfCoreOutbox() — analytics is a pure consumer, no outbox publishing

        return opts;
    }
}
