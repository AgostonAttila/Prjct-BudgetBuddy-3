using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Messages.Events.Budgets;

public record BudgetDeletedEvent : IntegrationEvent
{
    [JsonRequired] public Guid   BudgetId { get; init; }
    [JsonRequired] public string UserId   { get; init; } = string.Empty;
}
