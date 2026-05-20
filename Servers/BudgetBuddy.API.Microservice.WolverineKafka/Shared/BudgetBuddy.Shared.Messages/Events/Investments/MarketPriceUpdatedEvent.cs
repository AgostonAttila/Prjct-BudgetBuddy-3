using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Messages.Events.Investments;

public record MarketPriceUpdatedEvent : IntegrationEvent
{
    [JsonRequired] public string  Symbol   { get; init; } = string.Empty;
    [JsonRequired] public decimal Price    { get; init; }
    [JsonRequired] public string  Currency { get; init; } = string.Empty;
}
