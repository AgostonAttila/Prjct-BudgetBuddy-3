using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Messages.Events.Accounts;

/// <summary>
/// Published by the Accounts service in response to a TransactionCreatedEvent.
/// Success = true  → balance reserved OK.
/// Success = false → insufficient balance; Transactions service must compensate via saga.
/// </summary>
public sealed record AccountBalanceReservedEvent : IntegrationEvent
{
    [JsonRequired] public Guid    TransactionId { get; init; }
    [JsonRequired] public bool    Success       { get; init; }
                   public string? Reason        { get; init; }
}
