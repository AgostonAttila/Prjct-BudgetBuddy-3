using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Messages.Events.Users;

public record UserRegisteredEvent : IntegrationEvent
{
    [JsonRequired] public string  UserId    { get; init; } = string.Empty; // Keycloak sub
    [JsonRequired] public string  Email     { get; init; } = string.Empty;
                   public string? FirstName { get; init; }
                   public string? LastName  { get; init; }
}
