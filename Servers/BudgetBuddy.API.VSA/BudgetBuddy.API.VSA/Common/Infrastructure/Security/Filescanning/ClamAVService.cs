using nClam;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Security.Filescanning;

/// <summary>
/// ClamAV antivirus service implementation
/// Requires ClamAV daemon (clamd) to be running
/// </summary>
public class ClamAVService : IAntivirusService
{
    private readonly ClamClient _clamClient;
    private readonly ILogger<ClamAVService> _logger;
    private readonly bool _isEnabled;
    private readonly bool _isDevelopment;

    public ClamAVService(IConfiguration configuration, ILogger<ClamAVService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _isDevelopment = environment.IsDevelopment();

        var serverUrl = configuration["ClamAV:ServerUrl"] ?? "localhost";
        var serverPort = configuration.GetValue<int>("ClamAV:Port", 3310);
        _isEnabled = configuration.GetValue<bool>("ClamAV:Enabled", true);

        _clamClient = new ClamClient(serverUrl, serverPort);

        _logger.LogInformation(
            "ClamAV service initialized. Server: {ServerUrl}:{Port}, Enabled: {Enabled}, Environment: {Environment}",
            serverUrl, serverPort, _isEnabled, environment.EnvironmentName);
    }

    public async Task<AntivirusScanResult> ScanAsync(
        Stream fileStream,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        // FAIL-CLOSED: If ClamAV is disabled, block uploads in production
        if (!_isEnabled)
        {
            if (_isDevelopment)
            {
                _logger.LogWarning("ClamAV scanning is disabled in Development. File {FileName} not scanned", fileName ?? "unknown");
                return AntivirusScanResult.Clean(); // Allow in development only
            }

            // Production: CRITICAL security issue - ClamAV disabled
            _logger.LogCritical("SECURITY ALERT: ClamAV is disabled in production! File upload blocked: {FileName}", fileName ?? "unknown");
            throw new InvalidOperationException("Antivirus scanning is disabled in production environment. File uploads are not allowed.");
        }

        try
        {
            // Ping ClamAV server to check if it's available (FAIL-CLOSED behavior)
            var pingSuccess = await _clamClient.PingAsync(cancellationToken);
            if (!pingSuccess)
            {
                // Production: ClamAV unavailable - return error to block upload
                var logLevel = _isDevelopment ? LogLevel.Warning : LogLevel.Critical;
                _logger.Log(logLevel,
                    "SECURITY: ClamAV server is not responding for file {FileName}. Upload blocked.",
                    fileName ?? "unknown");

                return AntivirusScanResult.Error("Antivirus server is not available. File upload blocked for security.");
            }

            _logger.LogInformation("Scanning file {FileName} with ClamAV", fileName ?? "unknown");

            // Scan the file stream
            var scanResult = await _clamClient.SendAndScanFileAsync(fileStream, cancellationToken);

            switch (scanResult.Result)
            {
                case ClamScanResults.Clean:
                    _logger.LogInformation("File {FileName} is clean", fileName ?? "unknown");
                    return AntivirusScanResult.Clean();

                case ClamScanResults.VirusDetected:
                    _logger.LogWarning(
                        "Virus detected in file {FileName}: {VirusName}",
                        fileName ?? "unknown",
                        scanResult.InfectedFiles?.FirstOrDefault()?.VirusName ?? "Unknown");

                    var virusName = scanResult.InfectedFiles?.FirstOrDefault()?.VirusName ?? "Unknown malware";
                    return AntivirusScanResult.Infected(virusName);

                case ClamScanResults.Error:
                    _logger.LogError("ClamAV scan error for file {FileName}: {RawResult}",
                        fileName ?? "unknown", scanResult.RawResult);
                    return AntivirusScanResult.Error($"Scan error: {scanResult.RawResult}");

                default:
                    _logger.LogError("Unknown ClamAV scan result for file {FileName}: {Result}",
                        fileName ?? "unknown", scanResult.Result);
                    return AntivirusScanResult.Error("Unknown scan result");
            }
        }
        catch (Exception ex)
        {
            // FAIL-CLOSED: Any exception during scanning blocks the upload
            var logLevel = _isDevelopment ? LogLevel.Warning : LogLevel.Critical;
            _logger.Log(logLevel, ex,
                "SECURITY: Exception during ClamAV scan of file {FileName}. Upload blocked.",
                fileName ?? "unknown");

            return AntivirusScanResult.Error($"Antivirus scan failed: {ex.Message}. File upload blocked for security.");
        }
    }
}
