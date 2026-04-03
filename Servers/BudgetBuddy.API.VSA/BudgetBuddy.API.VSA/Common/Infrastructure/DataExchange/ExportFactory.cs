namespace BudgetBuddy.API.VSA.Common.Infrastructure.DataExchange;

public class ExportFactory(ICsvExportService csv, IExcelExportService excel) : IExportFactory
{
    public ExportResult Export<T>(
        ExportFormat format,
        IEnumerable<T> data,
        Dictionary<string, Func<T, object>> columnMappings,
        string fileNamePrefix,
        string sheetName = "Data")
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        return format switch
        {
            ExportFormat.Csv => new ExportResult(
                csv.ExportToCsv(data, columnMappings),
                $"{fileNamePrefix}_{timestamp}.csv",
                "text/csv"),
            _ => new ExportResult(
                excel.ExportToExcel(data, columnMappings, sheetName),
                $"{fileNamePrefix}_{timestamp}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        };
    }
}
