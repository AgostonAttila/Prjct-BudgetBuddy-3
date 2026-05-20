using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Messages.Events.ReferenceData;

public record CategoryChangedEvent : IntegrationEvent
{
    [JsonRequired] public Guid               CategoryId  { get; init; }
    [JsonRequired] public string             UserId      { get; init; } = string.Empty;
    [JsonRequired] public string             Name        { get; init; } = string.Empty;
                   public string?            Icon        { get; init; }
    [JsonRequired] public CategoryChangeType ChangeType  { get; init; }
}

public enum CategoryChangeType { Created, Updated, Deleted }
