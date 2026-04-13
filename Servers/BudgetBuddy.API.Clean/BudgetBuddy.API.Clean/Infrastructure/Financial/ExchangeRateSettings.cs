using System.ComponentModel.DataAnnotations;

namespace BudgetBuddy.Infrastructure.Financial;

public class ExchangeRateSettings
{
    public const string SectionName = "ExchangeRates";

    [Required]
    public string BaseUrl { get; init; } = "https://api.frankfurter.app";

    /// <summary>How long to cache exchange rates in Redis / distributed cache (L2).</summary>
    public int CacheDurationHours { get; init; } = 4;

    public int TimeoutSeconds { get; init; } = 10;
}
