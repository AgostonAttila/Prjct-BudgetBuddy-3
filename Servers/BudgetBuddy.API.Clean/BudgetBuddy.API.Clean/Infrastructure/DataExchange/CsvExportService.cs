using System.Text;

namespace BudgetBuddy.Infrastructure.DataExchange;

/// <summary>
/// CSV export service implementation.
/// Implements only CSV export methods - adheres to Interface Segregation Principle.
/// </summary>
public class CsvExportService : ICsvExportService
{
    public byte[] ExportToCsv<T>(IEnumerable<T> data, Dictionary<string, Func<T, object>> columnMappings)
    {
        var csv = new StringBuilder();

        // Add header row
        csv.AppendLine(string.Join(",", columnMappings.Keys.Select(EscapeCsvValue)));

        // Add data rows
        foreach (var item in data)
        {
            var values = columnMappings.Values
                .Select(mapping => mapping(item))
                .Select(value => value?.ToString() ?? string.Empty)
                .Select(EscapeCsvValue);

            csv.AppendLine(string.Join(",", values));
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        // Escape double quotes and wrap in quotes if contains comma, quote, or newline
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
