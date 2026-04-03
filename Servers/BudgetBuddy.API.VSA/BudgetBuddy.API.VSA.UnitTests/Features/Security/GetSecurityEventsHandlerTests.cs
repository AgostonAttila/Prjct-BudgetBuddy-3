using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Features.Security.GetSecurityEvents;
using FluentAssertions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Security;

public class GetSecurityEventsHandlerTests
{
    private readonly ISecurityEventService _securityEventService = Substitute.For<ISecurityEventService>();
    private readonly GetSecurityEventsHandler _handler;

    public GetSecurityEventsHandlerTests()
    {
        _handler = new GetSecurityEventsHandler(_securityEventService);
    }

    [Fact]
    public async Task Handle_CallsServiceWithCorrectArgs()
    {
        _securityEventService
            .GetRecentEventsAsync(25, SecurityEventSeverity.Warning, Arg.Any<CancellationToken>())
            .Returns(new List<SecurityEvent>());

        await _handler.Handle(new GetSecurityEventsQuery(25, SecurityEventSeverity.Warning), CancellationToken.None);

        await _securityEventService.Received(1).GetRecentEventsAsync(25, SecurityEventSeverity.Warning, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsMappedSecurityEvents()
    {
        var events = new List<SecurityEvent>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EventType = SecurityEventType.LoginFailure,
                Severity = SecurityEventSeverity.Warning,
                Message = "Failed login",
                IsAlert = false,
                IsReviewed = false,
                CreatedAt = Instant.FromUtc(2024, 6, 15, 10, 0)
            }
        };
        _securityEventService
            .GetRecentEventsAsync(Arg.Any<int>(), Arg.Any<SecurityEventSeverity?>(), Arg.Any<CancellationToken>())
            .Returns(events);

        var result = await _handler.Handle(new GetSecurityEventsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().EventType.Should().Be(SecurityEventType.LoginFailure);
    }
}
