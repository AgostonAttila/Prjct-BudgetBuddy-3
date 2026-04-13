namespace BudgetBuddy.Application.Common.Contracts;

/// <summary>
/// Service for exporting data to CSV format.
/// Follows Interface Segregation Principle - only CSV export methods.
/// </summary>
public interface ICsvExportService
{
    /// <summary>
    /// Exports data to CSV format with specified column mappings.
    /// </summary>
    /// <typeparam name="T">The type of data to export</typeparam>
    /// <param name="data">Collection of data to export</param>
    /// <param name="columnMappings">Dictionary mapping column names to data extractors</param>
    /// <returns>CSV file content as byte array</returns>
    byte[] ExportToCsv<T>(IEnumerable<T> data, Dictionary<string, Func<T, object>> columnMappings);
}
