using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.BudgetAlerts.GetBudgetAlerts;
using BudgetBuddy.API.VSA.Features.BudgetAlerts.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.BudgetAlerts;

public class GetBudgetAlertsHandlerTests
{
    private readonly IBudgetAlertService _alertService = Substitute.For<IBudgetAlertService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly GetBudgetAlertsHandler _handler;
    private const string UserId = "user-123";

    public GetBudgetAlertsHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _clock.GetCurrentInstant().Returns(Instant.FromUtc(2024, 6, 15, 10, 0));
        _handler = new GetBudgetAlertsHandler(_alertService, _currentUserService, _clock, NullLogger<GetBudgetAlertsHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenNoAlerts_ReturnsEmptyAlertsList()
    {
        _alertService.CalculateAlertsAsync(UserId, 2024, 6, Arg.Any<CancellationToken>())
            .Returns(new List<BudgetAlertDto>());

        var result = await _handler.Handle(new GetBudgetAlertsQuery(null, null), CancellationToken.None);

        result.Alerts.Should().BeEmpty();
        result.TotalAlerts.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithExplicitYearMonth_PassesThemToService()
    {
        _alertService.CalculateAlertsAsync(UserId, 2023, 12, Arg.Any<CancellationToken>())
            .Returns(new List<BudgetAlertDto>());

        await _handler.Handle(new GetBudgetAlertsQuery(Year: 2023, Month: 12), CancellationToken.None);

        await _alertService.Received(1).CalculateAlertsAsync(UserId, 2023, 12, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutYearMonth_UsesCurrentDate()
    {
        _alertService.CalculateAlertsAsync(UserId, 2024, 6, Arg.Any<CancellationToken>())
            .Returns(new List<BudgetAlertDto>());

        await _handler.Handle(new GetBudgetAlertsQuery(null, null), CancellationToken.None);

        await _alertService.Received(1).CalculateAlertsAsync(UserId, 2024, 6, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsAlertsFromService()
    {
        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var alerts = new List<BudgetAlertDto>
        {
            new(budgetId, categoryId, "Food", "", 100m, 150m, -50m, 150m, AlertLevel.Exceeded, "Budget exceeded by 50")
        };
        _alertService.CalculateAlertsAsync(UserId, 2024, 6, Arg.Any<CancellationToken>()).Returns(alerts);

        var result = await _handler.Handle(new GetBudgetAlertsQuery(null, null), CancellationToken.None);

        result.Alerts.Should().HaveCount(1);
        result.Alerts[0].AlertLevel.Should().Be(AlertLevel.Exceeded);
    }
}
