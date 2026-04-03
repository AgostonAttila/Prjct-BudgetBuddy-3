using NodaTime;

namespace BudgetBuddy.API.VSA.Common.Domain.Entities;

public class ExchangeRateSnapshot
{
    public string Currency { get; set; } = string.Empty; // e.g. HUF, EUR, GBP
    public LocalDate Date { get; set; }

    /// <summary>How many USD equals 1 unit of this currency. USD itself = 1.0.</summary>
    /// <remarks>
    /// Cross-rate formula: amount_in_target = amount × (RateToUsd[source] / RateToUsd[target])
    /// Example: 1000 HUF → EUR = 1000 × (0.0027 / 1.087) ≈ 2.48 EUR
    /// </remarks>
    public decimal RateToUsd { get; set; }

    public Instant CreatedAt { get; set; }
}
