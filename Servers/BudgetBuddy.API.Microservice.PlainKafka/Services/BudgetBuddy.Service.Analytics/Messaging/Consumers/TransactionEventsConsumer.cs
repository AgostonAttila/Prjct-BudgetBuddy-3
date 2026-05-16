using System.Text.Json;
using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.Analytics.Messaging.Consumers;

/// <summary>
/// Consumes transaction events, invalidates analytics cache AND updates the local TransactionReadModel.
/// </summary>
public sealed class TransactionEventsConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<TransactionEventsConsumer> logger)
    : KafkaMultiTopicConsumerBase(
        settings,
        [TopicNames.TransactionsCreated, TopicNames.TransactionsUpdated, TopicNames.TransactionsDeleted],
        ConsumerGroups.AnalyticsTransactions,
        scopeFactory,
        logger)
{
    protected override string? DlqTopic => TopicNames.TransactionsDlq;

    protected override async Task ProcessAsync(
        string topic, string? payload, string messageKey, long offset,
        IServiceProvider services, CancellationToken ct)
    {
        var cache = services.GetRequiredService<IUserCacheInvalidator>();
        var repo  = services.GetRequiredService<ITransactionReadModelRepository>();

        using var doc = JsonDocument.Parse(payload!);
        var root      = doc.RootElement;
        var userId    = root.GetProperty("UserId").GetString()!;

        await cache.InvalidateAsync(userId, ct);

        if (topic == TopicNames.TransactionsDeleted)
        {
            var transactionId = root.GetProperty("TransactionId").GetGuid();
            await repo.DeleteAsync(transactionId, ct);
        }
        else
        {
            var model = new TransactionReadModel
            {
                TransactionId   = root.GetProperty("TransactionId").GetGuid(),
                UserId          = userId,
                AccountId       = root.GetProperty("AccountId").GetGuid(),
                CategoryId      = TryGetGuid(root, "CategoryId"),
                TransactionType = (TransactionType)root.GetProperty("TransactionType").GetInt32(),
                Amount          = root.GetProperty("Amount").GetDecimal(),
                CurrencyCode    = root.GetProperty("CurrencyCode").GetString()!,
                TransactionDate = ParseLocalDate(root.GetProperty("TransactionDate")),
                SyncedAt        = DateTime.UtcNow,
            };
            await repo.UpsertAsync(model, ct);
        }
    }

    private static Guid? TryGetGuid(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null)
        {
            return prop.GetGuid();
        }

        return null;
    }

    private static LocalDate ParseLocalDate(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var parts = element.GetString()!.Split('-');
            return new LocalDate(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
        }
        var year  = element.GetProperty("year").GetInt32();
        var month = element.GetProperty("month").GetInt32();
        var day   = element.GetProperty("day").GetInt32();
        return new LocalDate(year, month, day);
    }
}
