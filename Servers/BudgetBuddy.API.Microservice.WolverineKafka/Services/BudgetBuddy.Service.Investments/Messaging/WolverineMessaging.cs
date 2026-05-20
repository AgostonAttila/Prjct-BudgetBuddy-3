using BudgetBuddy.Shared.Infrastructure.Extensions;
using BudgetBuddy.Shared.Messages.Events.Investments;
using BudgetBuddy.Shared.Messages.Topics;
using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.Kafka;

namespace BudgetBuddy.Service.Investments.Messaging;

internal static class WolverineMessaging
{
    internal static WolverineOptions AddInvestmentsMessaging(this WolverineOptions opts, IConfiguration configuration)
    {
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        opts.UseKafka(kafkaBootstrap)
            .DeadLetterQueueTopicName(TopicNames.InvestmentsDlq);

        opts.AddDefaultRetryPolicy();

        // Outbound: investment CRUD + market price events
        opts.PublishMessage<InvestmentCreatedEvent>().ToKafkaTopic(TopicNames.InvestmentsCreated);
        opts.PublishMessage<InvestmentUpdatedEvent>().ToKafkaTopic(TopicNames.InvestmentsUpdated);
        opts.PublishMessage<InvestmentDeletedEvent>().ToKafkaTopic(TopicNames.InvestmentsDeleted);
        opts.PublishMessage<MarketPriceUpdatedEvent>().ToKafkaTopic(TopicNames.MarketPriceUpdated);

        // Inbound: account snapshot handlers
        opts.ListenToKafkaTopic(TopicNames.AccountsCreated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.InvestmentsAccounts)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.AccountsUpdated)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.InvestmentsAccounts)
            .WithResilience();

        opts.ListenToKafkaTopic(TopicNames.AccountsDeleted)
            .ConfigureConsumer(cfg => cfg.GroupId = ConsumerGroups.InvestmentsAccounts)
            .WithResilience();

        opts.WithEfCoreOutbox(configuration.GetConnectionString("DefaultConnection")!);

        return opts;
    }
}
