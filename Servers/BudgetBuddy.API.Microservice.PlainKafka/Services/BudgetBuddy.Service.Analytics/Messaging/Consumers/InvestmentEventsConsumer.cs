using System.Text.Json;
using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Investments;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.Analytics.Messaging.Consumers;

/// <summary>
/// Consumes investment CRUD events to maintain a local InvestmentReadModel.
/// </summary>
public sealed class InvestmentEventsConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<InvestmentEventsConsumer> logger)
    : KafkaMultiTopicConsumerBase(
        settings,
        [TopicNames.InvestmentsCreated, TopicNames.InvestmentsUpdated, TopicNames.InvestmentsDeleted],
        ConsumerGroups.AnalyticsInvestments,
        scopeFactory,
        logger)
{
    protected override string? DlqTopic => TopicNames.InvestmentsDlq;

    protected override async Task ProcessAsync(
        string topic, string? payload, string messageKey, long offset,
        IServiceProvider services, CancellationToken ct)
    {
        var repo = services.GetRequiredService<IInvestmentReadModelRepository>();

        if (topic == TopicNames.InvestmentsDeleted)
        {
            var evt = JsonSerializer.Deserialize<InvestmentDeletedEvent>(payload!)!;
            await repo.DeleteAsync(evt.InvestmentId, ct);
        }
        else
        {
            using var doc = JsonDocument.Parse(payload!);
            var root = doc.RootElement;
            var model = new InvestmentReadModel
            {
                InvestmentId  = root.GetProperty("InvestmentId").GetGuid(),
                UserId        = root.GetProperty("UserId").GetString()!,
                Symbol        = root.GetProperty("Symbol").GetString()!,
                Name          = root.GetProperty("Name").GetString()!,
                Type          = (InvestmentType)root.GetProperty("Type").GetInt32(),
                Quantity      = root.GetProperty("Quantity").GetDecimal(),
                PurchasePrice = root.GetProperty("PurchasePrice").GetDecimal(),
                CurrencyCode  = root.GetProperty("CurrencyCode").GetString()!,
                PurchaseDate  = ParseLocalDate(root.GetProperty("PurchaseDate")),
                SyncedAt      = DateTime.UtcNow,
            };
            await repo.UpsertAsync(model, ct);
        }
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
