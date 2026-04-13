namespace BudgetBuddy.Application.Common.Contracts;

/// <summary>
/// Service for scanning files for viruses and malware
/// </summary>
public interface IAntivirusService
{
    /// <summary>
    /// Scans a file stream for viruses
    /// </summary>
    /// <param name="fileStream">The file stream to scan</param>
    /// <param name="fileName">Optional file name for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scan result indicating if the file is clean</returns>
    Task<AntivirusScanResult> ScanAsync(Stream fileStream, string? fileName = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an antivirus scan
/// </summary>
public record AntivirusScanResult
{
    /// <summary>
    /// Indicates if the file is clean (no viruses detected)
    /// </summary>
    public required bool IsClean { get; init; }

    /// <summary>
    /// Name of the virus/malware detected (if any)
    /// </summary>
    public string? VirusName { get; init; }

    /// <summary>
    /// Detailed scan result message
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Indicates if the scan failed due to an error
    /// </summary>
    public bool IsScanError { get; init; }

    /// <summary>
    /// Error message if scan failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a result for a clean file
    /// </summary>
    public static AntivirusScanResult Clean() => new()
    {
        IsClean = true,
        Message = "File is clean"
    };

    /// <summary>
    /// Creates a result for an infected file
    /// </summary>
    public static AntivirusScanResult Infected(string virusName) => new()
    {
        IsClean = false,
        VirusName = virusName,
        Message = $"Virus detected: {virusName}"
    };

    /// <summary>
    /// Creates a result for a scan error
    /// </summary>
    public static AntivirusScanResult Error(string errorMessage) => new()
    {
        IsClean = false,
        IsScanError = true,
        ErrorMessage = errorMessage,
        Message = "Antivirus scan failed"
    };
}
