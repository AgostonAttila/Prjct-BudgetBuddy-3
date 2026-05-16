using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Integration;
using NodaTime;

namespace BudgetBuddy.Shared.Messages.Events.Transactions;

public record TransactionDeletedEvent : IntegrationEvent
{
    [JsonRequired] public Guid            TransactionId   { get; init; }
    [JsonRequired] public string          UserId          { get; init; } = string.Empty;
    [JsonRequired] public Guid            AccountId       { get; init; }
    [JsonRequired] public decimal         Amount          { get; init; }
    [JsonRequired] public string          CurrencyCode    { get; init; } = string.Empty;
    [JsonRequired] public TransactionType TransactionType { get; init; }
                   public Guid?           CategoryId      { get; init; }
    [JsonRequired] public LocalDate       TransactionDate { get; init; }
}
