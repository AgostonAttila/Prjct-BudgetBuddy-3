using NodaTime;

namespace BudgetBuddy.Module.Investments.Domain;

public class PriceSnapshot
{
    public string Symbol { get; set; } = string.Empty;
    public LocalDate Date { get; set; }
    public decimal ClosePrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public Instant CreatedAt { get; set; }
}
