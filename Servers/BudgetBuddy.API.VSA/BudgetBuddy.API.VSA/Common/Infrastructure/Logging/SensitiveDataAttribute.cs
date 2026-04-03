namespace BudgetBuddy.API.VSA.Common.Infrastructure.Logging;

/// <summary>
/// Marks a property as containing sensitive data that should be masked in logs
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SensitiveDataAttribute : Attribute
{
    public MaskingStrategy Strategy { get; set; } = MaskingStrategy.Full;
}

public enum MaskingStrategy
{
    /// <summary>
    /// Fully mask the value: "secret" → "******"
    /// </summary>
    Full,

    /// <summary>
    /// Show first 2 characters: "secret" → "se****"
    /// </summary>
    Partial,

    /// <summary>
    /// Show first and last 2 characters: "secret" → "se**et"
    /// </summary>
    PartialBoth,

    /// <summary>
    /// Email masking: "john@example.com" → "jo***@example.com"
    /// </summary>
    Email
}
