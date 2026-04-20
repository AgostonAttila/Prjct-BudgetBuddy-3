using NodaTime;

namespace BudgetBuddy.Module.Investments.Domain;

public class ExchangeRateSnapshot
{
    public string Currency { get; set; } = string.Empty;
    public LocalDate Date { get; set; }

    /// <summary>How many USD equals 1 unit of this currency. USD itself = 1.0.</summary>
    public decimal RateToUsd { get; set; }

    public Instant CreatedAt { get; set; }
}
