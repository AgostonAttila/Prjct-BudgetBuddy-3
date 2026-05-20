using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Integration;
using NodaTime;

namespace BudgetBuddy.Shared.Messages.Events.Investments;

public record InvestmentCreatedEvent : IntegrationEvent
{
    [JsonRequired] public Guid           InvestmentId   { get; init; }
    [JsonRequired] public string         UserId         { get; init; } = string.Empty;
    [JsonRequired] public string         Symbol         { get; init; } = string.Empty;
    [JsonRequired] public string         Name           { get; init; } = string.Empty;
    [JsonRequired] public InvestmentType Type           { get; init; }
    [JsonRequired] public decimal        Quantity       { get; init; }
    [JsonRequired] public decimal        PurchasePrice  { get; init; }
    [JsonRequired] public string         CurrencyCode   { get; init; } = string.Empty;
    [JsonRequired] public LocalDate      PurchaseDate   { get; init; }
}
