namespace BudgetBuddy.Shared.Infrastructure.DataExchange;

public record ExportResult(byte[] Content, string FileName, string ContentType);

public interface IExportFactory
{
    ExportResult Export<T>(
        ExportFormat format,
        IEnumerable<T> data,
        Dictionary<string, Func<T, object>> columnMappings,
        string fileNamePrefix,
        string sheetName = "Data");
}
