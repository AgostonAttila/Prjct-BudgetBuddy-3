using NodaTime;

namespace BudgetBuddy.Domain.Entities;

public class PriceSnapshot
{
    public string Symbol { get; set; } = string.Empty;
    public LocalDate Date { get; set; }
    public decimal ClosePrice { get; set; }
    public string Currency { get; set; } = string.Empty; // always USD
    public string Source { get; set; } = string.Empty;   // "yahoo" | "coingecko"
    public Instant CreatedAt { get; set; }
}
