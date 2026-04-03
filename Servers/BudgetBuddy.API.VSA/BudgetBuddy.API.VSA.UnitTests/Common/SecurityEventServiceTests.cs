using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace BudgetBuddy.API.VSA.UnitTests.Common;

public class SecurityEventServiceTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly SecurityEventService _sut;

    public SecurityEventServiceTests()
    {
        _sut = new SecurityEventService(_db, NullLogger<SecurityEventService>.Instance);
    }

    // -------------------------------------------------------------------------
    // LogEventAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LogEvent_PersistsEventToDatabase()
    {
        await _sut.LogEventAsync(
            SecurityEventType.LoginFailure,
            SecurityEventSeverity.Warning,
            "Bad password",
            userId: "u1",
            ipAddress: "1.2.3.4");

        var saved = _db.SecurityEvents.Single();
        saved.EventType.Should().Be(SecurityEventType.LoginFailure);
        saved.Severity.Should().Be(SecurityEventSeverity.Warning);
        saved.Message.Should().Be("Bad password");
        saved.UserId.Should().Be("u1");
        saved.IpAddress.Should().Be("1.2.3.4");
    }

    [Fact]
    public async Task LogEvent_SetsIsAlertTrue_ForHighSeverity()
    {
        await _sut.LogEventAsync(SecurityEventType.AccountLockout, SecurityEventSeverity.High, "Locked");

        _db.SecurityEvents.Single().IsAlert.Should().BeTrue();
    }

    [Fact]
    public async Task LogEvent_SetsIsAlertTrue_ForCriticalSeverity()
    {
        await _sut.LogEventAsync(SecurityEventType.BruteForceDetected, SecurityEventSeverity.Critical, "Brute force");

        _db.SecurityEvents.Single().IsAlert.Should().BeTrue();
    }

    [Fact]
    public async Task LogEvent_SetsIsAlertFalse_ForWarningSeverity()
    {
        await _sut.LogEventAsync(SecurityEventType.LoginFailure, SecurityEventSeverity.Warning, "Single failure");

        _db.SecurityEvents.Single().IsAlert.Should().BeFalse();
    }

    [Fact]
    public async Task LogEvent_SetsIsAlertFalse_ForInfoSeverity()
    {
        await _sut.LogEventAsync(SecurityEventType.LoginSuccess, SecurityEventSeverity.Info, "OK");

        _db.SecurityEvents.Single().IsAlert.Should().BeFalse();
    }

    [Fact]
    public async Task LogEvent_SetsIsReviewedFalse_ByDefault()
    {
        await _sut.LogEventAsync(SecurityEventType.LoginSuccess, SecurityEventSeverity.Info, "OK");

        _db.SecurityEvents.Single().IsReviewed.Should().BeFalse();
    }

    [Fact]
    public async Task LogEvent_SerializesMetadataAsJson()
    {
        await _sut.LogEventAsync(
            SecurityEventType.RateLimitExceeded,
            SecurityEventSeverity.Warning,
            "Rate limited",
            metadata: new { Count = 42 });

        var saved = _db.SecurityEvents.Single();
        saved.Metadata.Should().Contain("42");
    }

    [Fact]
    public async Task LogEvent_DoesNotThrow_WhenDbFails()
    {
        // Dispose the DB so any save attempt throws
        _db.Dispose();

        var act = () => _sut.LogEventAsync(SecurityEventType.LoginFailure, SecurityEventSeverity.Warning, "fail");

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------------
    // DetectBruteForceAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DetectBruteForce_ReturnsFalse_WhenFailuresBelowThreshold()
    {
        await SeedLoginFailures(userId: "u1", count: 2, minutesAgo: 1);

        var result = await _sut.DetectBruteForceAsync("u1", null, TimeSpan.FromMinutes(5), threshold: 3);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DetectBruteForce_ReturnsTrue_WhenFailuresReachThreshold()
    {
        await SeedLoginFailures(userId: "u1", count: 3, minutesAgo: 1);

        var result = await _sut.DetectBruteForceAsync("u1", null, TimeSpan.FromMinutes(5), threshold: 3);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DetectBruteForce_IgnoresEventsOutsideTimeWindow()
    {
        await SeedLoginFailures(userId: "u1", count: 5, minutesAgo: 10);

        // 5-minute window excludes events from 10 minutes ago
        var result = await _sut.DetectBruteForceAsync("u1", null, TimeSpan.FromMinutes(5), threshold: 3);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DetectBruteForce_FiltersBy_IpAddress_WhenNoUserId()
    {
        await SeedLoginFailures(ipAddress: "10.0.0.1", count: 5, minutesAgo: 1);

        var result = await _sut.DetectBruteForceAsync(null, "10.0.0.1", TimeSpan.FromMinutes(5), threshold: 3);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DetectBruteForce_LogsBruteForceEvent_WhenThresholdReached()
    {
        await SeedLoginFailures(userId: "u1", count: 3, minutesAgo: 1);

        await _sut.DetectBruteForceAsync("u1", null, TimeSpan.FromMinutes(5), threshold: 3);

        // LogEventAsync is called internally — a BruteForceDetected event must be persisted
        _db.SecurityEvents
            .Any(e => e.EventType == SecurityEventType.BruteForceDetected)
            .Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // GetActiveAlertsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetActiveAlerts_ReturnsOnlyUnreviewedHighPlusSeverity()
    {
        _db.SecurityEvents.AddRange(
            MakeEvent(SecurityEventSeverity.High,     isAlert: true,  isReviewed: false),  // included
            MakeEvent(SecurityEventSeverity.Critical, isAlert: true,  isReviewed: false),  // included
            MakeEvent(SecurityEventSeverity.High,     isAlert: true,  isReviewed: true),   // reviewed → excluded
            MakeEvent(SecurityEventSeverity.Warning,  isAlert: false, isReviewed: false),  // low severity → excluded
            MakeEvent(SecurityEventSeverity.Info,     isAlert: false, isReviewed: false)   // low severity → excluded
        );
        await _db.SaveChangesAsync();

        var alerts = await _sut.GetActiveAlertsAsync();

        alerts.Should().HaveCount(2);
        alerts.Should().OnlyContain(e => e.IsAlert && !e.IsReviewed && e.Severity >= SecurityEventSeverity.High);
    }

    [Fact]
    public async Task GetActiveAlerts_ReturnsEmptyList_WhenNoAlerts()
    {
        var alerts = await _sut.GetActiveAlertsAsync();

        alerts.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // MarkAsReviewedAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task MarkAsReviewed_SetsIsReviewedTrue()
    {
        var ev = MakeEvent(SecurityEventSeverity.High, isAlert: true, isReviewed: false);
        _db.SecurityEvents.Add(ev);
        await _db.SaveChangesAsync();

        await _sut.MarkAsReviewedAsync(ev.Id);

        _db.SecurityEvents.Single(e => e.Id == ev.Id).IsReviewed.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsReviewed_DoesNotThrow_WhenEventNotFound()
    {
        var act = () => _sut.MarkAsReviewedAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task SeedLoginFailures(string? userId = null, string? ipAddress = null, int count = 1, int minutesAgo = 1)
    {
        var createdAt = Instant.FromDateTimeUtc(DateTime.UtcNow.AddMinutes(-minutesAgo));

        for (var i = 0; i < count; i++)
        {
            _db.SecurityEvents.Add(new SecurityEvent
            {
                Id = Guid.NewGuid(),
                EventType = SecurityEventType.LoginFailure,
                Severity = SecurityEventSeverity.Warning,
                Message = "Failed login",
                UserId = userId,
                IpAddress = ipAddress,
                CreatedAt = createdAt
            });
        }

        await _db.SaveChangesAsync();
    }

    private static SecurityEvent MakeEvent(SecurityEventSeverity severity, bool isAlert, bool isReviewed) =>
        new()
        {
            Id = Guid.NewGuid(),
            EventType = SecurityEventType.LoginFailure,
            Severity = severity,
            Message = "test",
            IsAlert = isAlert,
            IsReviewed = isReviewed,
            CreatedAt = Instant.FromDateTimeUtc(DateTime.UtcNow)
        };

    public void Dispose() => _db.Dispose();
}
