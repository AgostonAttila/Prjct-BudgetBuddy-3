using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

/// <summary>
/// Shared JSON options for test deserialization.
/// Matches the API's serialization settings:
///   - Enums as strings (JsonStringEnumConverter)
///   - NodaTime types (LocalDate, Instant, etc.)
/// </summary>
public static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
}
