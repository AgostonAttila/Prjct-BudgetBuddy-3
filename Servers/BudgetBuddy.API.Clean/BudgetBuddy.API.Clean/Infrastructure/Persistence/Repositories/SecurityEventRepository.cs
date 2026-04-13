using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Infrastructure.Persistence.Repositories;

public class SecurityEventRepository(AppDbContext context) : ISecurityEventRepository
{
    // ⚠️ ARCHITECTURAL NOTE: Intentionally bypasses IUnitOfWork.
    // Security events are critical audit records that must be persisted immediately and atomically,
    // independent of any surrounding business transaction. They must NOT be batched into a
    // larger SaveChanges call — a failed outer transaction would silently drop the audit trail.
    public async Task AddAsync(SecurityEvent securityEvent, CancellationToken ct = default)
    {
        await context.SecurityEvents.AddAsync(securityEvent, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<int> CountFailuresAsync(
        string? userId, string? ipAddress, Instant cutoffTime, CancellationToken ct = default)
    {
        var query = context.SecurityEvents
            .Where(e => e.CreatedAt >= cutoffTime)
            .Where(e => e.EventType == SecurityEventType.LoginFailure ||
                        e.EventType == SecurityEventType.TwoFactorFailure);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(e => e.UserId == userId);

        if (!string.IsNullOrEmpty(ipAddress))
            query = query.Where(e => e.IpAddress == ipAddress);

        return await query.CountAsync(ct);
    }

    public async Task<List<SecurityEvent>> GetRecentAsync(
        int count, SecurityEventSeverity? minSeverity, CancellationToken ct = default)
    {
        var query = context.SecurityEvents.AsNoTracking().AsQueryable();

        if (minSeverity.HasValue)
            query = query.Where(e => e.Severity >= minSeverity.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<List<SecurityEvent>> GetActiveAlertsAsync(CancellationToken ct = default)
    {
        return await context.SecurityEvents
            .AsNoTracking()
            .Where(e => e.IsAlert && !e.IsReviewed && e.Severity >= SecurityEventSeverity.High)
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
    }

    // ⚠️ ARCHITECTURAL NOTE: See AddAsync above — same reasoning applies here.
    public async Task MarkAsReviewedAsync(Guid eventId, CancellationToken ct = default)
    {
        var securityEvent = await context.SecurityEvents.FindAsync(new object[] { eventId }, ct);
        if (securityEvent != null)
        {
            securityEvent.IsReviewed = true;
            await context.SaveChangesAsync(ct);
        }
    }

    // ExecuteDeleteAsync intentionally bypasses EF Core change tracking (no AuditLogInterceptor).
    // Auditing the deletion of old audit records is unnecessary — it would create an infinite
    // retention loop where cleanup events themselves accumulate and never get cleaned up.
    public async Task<int> DeleteOldAsync(Instant cutoffTime, CancellationToken ct = default)
    {
        return await context.SecurityEvents
            .Where(e => e.CreatedAt < cutoffTime && e.IsReviewed)
            .ExecuteDeleteAsync(ct);
    }
}
