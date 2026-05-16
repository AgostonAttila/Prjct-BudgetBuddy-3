using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Messages.Events.Budgets;

public record BudgetAlertTriggeredEvent : IntegrationEvent
{
    [JsonRequired] public Guid    BudgetId         { get; init; }
    [JsonRequired] public string  UserId           { get; init; } = string.Empty;
    [JsonRequired] public string  BudgetName       { get; init; } = string.Empty;
    [JsonRequired] public decimal Limit            { get; init; }
    [JsonRequired] public decimal CurrentSpending  { get; init; }
    [JsonRequired] public decimal ThresholdPercent { get; init; }
                   public string? UserEmail        { get; init; }
}
