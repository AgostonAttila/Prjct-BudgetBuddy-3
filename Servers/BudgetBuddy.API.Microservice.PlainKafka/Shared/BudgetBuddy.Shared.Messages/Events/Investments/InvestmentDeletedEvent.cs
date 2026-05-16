using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Messages.Events.Investments;

public record InvestmentDeletedEvent : IntegrationEvent
{
    [JsonRequired] public Guid   InvestmentId { get; init; }
    [JsonRequired] public string UserId       { get; init; } = string.Empty;
}
