using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Messages.Events.Budgets;

public record BudgetCreatedEvent : IntegrationEvent
{
    [JsonRequired] public Guid    BudgetId     { get; init; }
    [JsonRequired] public string  UserId       { get; init; } = string.Empty;
    [JsonRequired] public Guid    CategoryId   { get; init; }
    [JsonRequired] public decimal Amount       { get; init; }
    [JsonRequired] public string  CurrencyCode { get; init; } = string.Empty;
    [JsonRequired] public int     Year         { get; init; }
    [JsonRequired] public int     Month        { get; init; }
}
