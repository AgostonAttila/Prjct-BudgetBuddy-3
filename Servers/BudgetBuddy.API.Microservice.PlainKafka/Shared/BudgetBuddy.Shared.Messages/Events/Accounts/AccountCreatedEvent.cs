using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Messages.Events.Accounts;

public record AccountCreatedEvent : IntegrationEvent
{
    [JsonRequired] public Guid    AccountId      { get; init; }
    [JsonRequired] public string  UserId         { get; init; } = string.Empty;
    [JsonRequired] public string  Name           { get; init; } = string.Empty;
    [JsonRequired] public string  Currency       { get; init; } = string.Empty;
    [JsonRequired] public decimal InitialBalance { get; init; }
}
