using NodaTime.Text;

namespace BudgetBuddy.Application.Common.Extensions;

public static class DateParsingExtensions
{
    /// <summary>
    /// Parses an ISO date string (yyyy-MM-dd) or throws a <see cref="DomainValidationException"/> with a clear message.
    /// </summary>
    public static LocalDate ParseIsoDateOrThrow(this string value, string paramName)
    {
        var result = LocalDatePattern.Iso.Parse(value);
        if (!result.Success)
            throw new DomainValidationException(
                $"Invalid date format for '{paramName}': '{value}'. Expected ISO format (yyyy-MM-dd).");

        return result.Value;
    }

    /// <summary>
    /// Parses an ISO date string if non-empty, returns null otherwise. Never throws.
    /// </summary>
    public static LocalDate? TryParseIsoDate(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var result = LocalDatePattern.Iso.Parse(value);
        return result.Success ? result.Value : null;
    }
}
