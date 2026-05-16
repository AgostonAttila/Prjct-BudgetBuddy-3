using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Messages.Events.Accounts;

public record AccountDeletedEvent : IntegrationEvent
{
    [JsonRequired] public Guid   AccountId { get; init; }
    [JsonRequired] public string UserId    { get; init; } = string.Empty;
}
