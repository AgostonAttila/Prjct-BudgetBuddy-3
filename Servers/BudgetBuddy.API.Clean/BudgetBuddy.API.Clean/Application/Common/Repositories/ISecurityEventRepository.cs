namespace BudgetBuddy.Application.Common.Repositories;

public interface ISecurityEventRepository
{
    Task AddAsync(SecurityEvent securityEvent, CancellationToken ct = default);
    Task<int> CountFailuresAsync(string? userId, string? ipAddress, Instant cutoffTime, CancellationToken ct = default);
    Task<List<SecurityEvent>> GetRecentAsync(int count, SecurityEventSeverity? minSeverity, CancellationToken ct = default);
    Task<List<SecurityEvent>> GetActiveAlertsAsync(CancellationToken ct = default);
    Task MarkAsReviewedAsync(Guid eventId, CancellationToken ct = default);
    Task<int> DeleteOldAsync(Instant cutoffTime, CancellationToken ct = default);
}
