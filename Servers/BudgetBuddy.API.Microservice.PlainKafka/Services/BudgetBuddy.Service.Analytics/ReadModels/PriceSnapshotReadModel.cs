using NodaTime;

namespace BudgetBuddy.Service.Analytics.ReadModels;

/// <summary>
/// Local read model for daily closing prices received via MarketPriceUpdatedEvent.
/// Prices are always stored in USD. Composite PK: (Symbol, Date).
/// </summary>
public class PriceSnapshotReadModel
{
    public string    Symbol   { get; set; } = string.Empty;
    public LocalDate Date     { get; set; }
    public decimal   PriceUsd { get; set; }
    public Instant   SyncedAt { get; set; }
}
