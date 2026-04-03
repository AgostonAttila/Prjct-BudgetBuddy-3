using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Dashboard.GetDashboard;
using BudgetBuddy.API.VSA.Features.Dashboard.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Dashboard;

public class GetDashboardHandlerTests
{
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserCurrencyService _userCurrencyService = Substitute.For<IUserCurrencyService>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly GetDashboardHandler _handler;
    private const string UserId = "user-123";

    public GetDashboardHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _userCurrencyService.GetDisplayCurrencyAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns("USD");

        // Fix the clock to a known time
        _clock.GetCurrentInstant().Returns(Instant.FromUtc(2024, 6, 15, 12, 0));

        var accountsSummary = new DashboardAccountsSummary(1000m, 2, "USD");
        var monthSummary = new DashboardMonthSummary(2024, 6, "June", 2000m, 1000m, 1000m, 15);
        var budgetSummary = new DashboardBudgetSummary(1500m, 800m, 700m, 53m, 0, 3);

        _dashboardService.GetAccountsSummaryAsync(UserId, "USD", Arg.Any<CancellationToken>()).Returns(accountsSummary);
        _dashboardService.GetMonthSummaryAsync(UserId, 2024, 6, "USD", Arg.Any<CancellationToken>()).Returns(monthSummary);
        _dashboardService.GetBudgetSummaryAsync(UserId, 2024, 6, "USD", Arg.Any<CancellationToken>()).Returns(budgetSummary);
        _dashboardService.GetTopCategoriesAsync(UserId, Arg.Any<LocalDate>(), Arg.Any<LocalDate>(), 5, "USD", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DashboardTopCategory>());
        _dashboardService.GetRecentTransactionsAsync(UserId, 10, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DashboardRecentTransaction>());

        _handler = new GetDashboardHandler(_dashboardService, _currentUserService, _userCurrencyService, _clock, NullLogger<GetDashboardHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CallsDashboardServiceComponents()
    {
        await _handler.Handle(new GetDashboardQuery("USD"), CancellationToken.None);

        await _dashboardService.Received(1).GetAccountsSummaryAsync(UserId, "USD", Arg.Any<CancellationToken>());
        await _dashboardService.Received(1).GetMonthSummaryAsync(UserId, 2024, 6, "USD", Arg.Any<CancellationToken>());
        await _dashboardService.Received(1).GetBudgetSummaryAsync(UserId, 2024, 6, "USD", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsDashboardResponse()
    {
        var result = await _handler.Handle(new GetDashboardQuery("USD"), CancellationToken.None);

        result.Should().NotBeNull();
        result.DisplayCurrency.Should().Be("USD");
        result.AccountsSummary.TotalBalance.Should().Be(1000m);
    }

}
