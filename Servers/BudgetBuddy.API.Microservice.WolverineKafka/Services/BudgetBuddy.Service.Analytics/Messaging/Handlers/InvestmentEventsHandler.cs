using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Investments;

namespace BudgetBuddy.Service.Analytics.Messaging.Handlers;

/// <summary>
/// Maintains the InvestmentReadModel.
/// Replaces <c>InvestmentEventsConsumer</c> (KafkaMultiTopicConsumerBase).
/// </summary>
[SupportedSchemaVersions("1")]
public static class InvestmentEventsHandler
{
    public static async Task Handle(
        InvestmentCreatedEvent evt,
        IInvestmentReadModelRepository repo,
        CancellationToken ct)
    {
        await repo.UpsertAsync(new InvestmentReadModel
        {
            InvestmentId  = evt.InvestmentId,
            UserId        = evt.UserId,
            Symbol        = evt.Symbol,
            Name          = evt.Name,
            Type          = evt.Type,
            Quantity      = evt.Quantity,
            PurchasePrice = evt.PurchasePrice,
            CurrencyCode  = evt.CurrencyCode,
            PurchaseDate  = evt.PurchaseDate,
            SyncedAt      = DateTime.UtcNow,
        }, ct);
    }

    public static async Task Handle(
        InvestmentUpdatedEvent evt,
        IInvestmentReadModelRepository repo,
        CancellationToken ct)
    {
        await repo.UpsertAsync(new InvestmentReadModel
        {
            InvestmentId  = evt.InvestmentId,
            UserId        = evt.UserId,
            Symbol        = evt.Symbol,
            Name          = evt.Name,
            Type          = evt.Type,
            Quantity      = evt.Quantity,
            PurchasePrice = evt.PurchasePrice,
            CurrencyCode  = evt.CurrencyCode,
            PurchaseDate  = evt.PurchaseDate,
            SyncedAt      = DateTime.UtcNow,
        }, ct);
    }

    public static async Task Handle(
        InvestmentDeletedEvent evt,
        IInvestmentReadModelRepository repo,
        CancellationToken ct)
    {
        await repo.DeleteAsync(evt.InvestmentId, ct);
    }
}
