using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Features.Security.GetSecurityAlerts;
using FluentAssertions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Security;

public class GetSecurityAlertsHandlerTests
{
    private readonly ISecurityEventService _securityEventService = Substitute.For<ISecurityEventService>();
    private readonly GetSecurityAlertsHandler _handler;

    public GetSecurityAlertsHandlerTests()
    {
        _handler = new GetSecurityAlertsHandler(_securityEventService);
    }

    [Fact]
    public async Task Handle_CallsGetActiveAlertsService()
    {
        _securityEventService.GetActiveAlertsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<SecurityEvent>());

        await _handler.Handle(new GetSecurityAlertsQuery(), CancellationToken.None);

        await _securityEventService.Received(1).GetActiveAlertsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsMappedAlerts()
    {
        var alerts = new List<SecurityEvent>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EventType = SecurityEventType.BruteForceDetected,
                Severity = SecurityEventSeverity.Critical,
                Message = "Brute force detected",
                IsAlert = true,
                IsReviewed = false,
                CreatedAt = Instant.FromUtc(2024, 6, 15, 10, 0)
            }
        };
        _securityEventService.GetActiveAlertsAsync(Arg.Any<CancellationToken>()).Returns(alerts);

        var result = await _handler.Handle(new GetSecurityAlertsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Severity.Should().Be(SecurityEventSeverity.Critical);
    }
}
