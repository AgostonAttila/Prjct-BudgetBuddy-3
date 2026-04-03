using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Reports.GetMonthlySummary;
using BudgetBuddy.API.VSA.Features.Reports.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Reports;

public class GetMonthlySummaryHandlerTests
{
    private readonly IReportService _reportService = Substitute.For<IReportService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserCurrencyService _userCurrencyService = Substitute.For<IUserCurrencyService>();
    private readonly GetMonthlySummaryHandler _handler;
    private const string UserId = "user-123";

    public GetMonthlySummaryHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _userCurrencyService.GetDisplayCurrencyAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns("USD");
        _handler = new GetMonthlySummaryHandler(_reportService, _currentUserService, _userCurrencyService, NullLogger<GetMonthlySummaryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CallsReportServiceWithCorrectArgs()
    {
        _reportService
            .CalculateMonthlySummaryAsync(UserId, 2024, 6, null, "USD", Arg.Any<CancellationToken>())
            .Returns((1000m, 500m, 500m, 2000m, 2500m, 10, new List<CategorySummary>(), new List<DailySummary>()));

        await _handler.Handle(new GetMonthlySummaryQuery(2024, 6), CancellationToken.None);

        await _reportService.Received(1).CalculateMonthlySummaryAsync(UserId, 2024, 6, null, "USD", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsMappedSummaryResponse()
    {
        _reportService
            .CalculateMonthlySummaryAsync(UserId, 2024, 3, null, "USD", Arg.Any<CancellationToken>())
            .Returns((1500m, 800m, 700m, 3000m, 3700m, 15, new List<CategorySummary>(), new List<DailySummary>()));

        var result = await _handler.Handle(new GetMonthlySummaryQuery(2024, 3), CancellationToken.None);

        result.Year.Should().Be(2024);
        result.Month.Should().Be(3);
        result.TotalIncome.Should().Be(1500m);
        result.TotalExpense.Should().Be(800m);
        result.NetIncome.Should().Be(700m);
    }
}
