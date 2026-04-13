using ClosedXML.Excel;

namespace BudgetBuddy.Infrastructure.DataExchange;

/// <summary>
/// Excel export service implementation.
/// Implements only Excel export methods - adheres to Interface Segregation Principle.
/// </summary>
public class ExcelExportService : IExcelExportService
{
    public byte[] ExportToExcel<T>(IEnumerable<T> data, Dictionary<string, Func<T, object>> columnMappings, string sheetName = "Data")
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Add headers
        var columnIndex = 1;
        foreach (var columnName in columnMappings.Keys)
        {
            worksheet.Cell(1, columnIndex).Value = columnName;
            worksheet.Cell(1, columnIndex).Style.Font.Bold = true;
            columnIndex++;
        }

        // Add data rows
        var rowIndex = 2;
        foreach (var item in data)
        {
            columnIndex = 1;
            foreach (var mapping in columnMappings.Values)
            {
                var value = mapping(item);

                if (value != null)
                {
                    worksheet.Cell(rowIndex, columnIndex).Value = value switch
                    {
                        DateTime dateTime => dateTime,
                        decimal or double or float or int or long => Convert.ToDouble(value),
                        bool boolValue => boolValue,
                        _ => value.ToString()
                    };
                }

                columnIndex++;
            }
            rowIndex++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
