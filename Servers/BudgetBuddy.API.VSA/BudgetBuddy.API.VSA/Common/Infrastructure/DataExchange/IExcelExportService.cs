namespace BudgetBuddy.API.VSA.Common.Infrastructure.DataExchange;

/// <summary>
/// Service for exporting data to Excel format.
/// Follows Interface Segregation Principle - only Excel export methods.
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Exports data to Excel format with specified column mappings.
    /// </summary>
    /// <typeparam name="T">The type of data to export</typeparam>
    /// <param name="data">Collection of data to export</param>
    /// <param name="columnMappings">Dictionary mapping column names to data extractors</param>
    /// <param name="sheetName">Name of the Excel sheet (default: "Data")</param>
    /// <returns>Excel file content as byte array</returns>
    byte[] ExportToExcel<T>(IEnumerable<T> data, Dictionary<string, Func<T, object>> columnMappings, string sheetName = "Data");
}
