using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Reports.GetInvestmentPerformance;
using BudgetBuddy.API.VSA.Features.Reports.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Reports;

public class GetInvestmentPerformanceHandlerTests
{
    private readonly IReportService _reportService = Substitute.For<IReportService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserCurrencyService _userCurrencyService = Substitute.For<IUserCurrencyService>();
    private readonly GetInvestmentPerformanceHandler _handler;
    private const string UserId = "user-123";

    public GetInvestmentPerformanceHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _userCurrencyService.GetDisplayCurrencyAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns("USD");
        _handler = new GetInvestmentPerformanceHandler(_reportService, _currentUserService, _userCurrencyService, NullLogger<GetInvestmentPerformanceHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CallsReportServiceWithCorrectArgs()
    {
        _reportService
            .CalculateInvestmentPerformanceAsync(UserId, null, null, null, "USD", Arg.Any<CancellationToken>())
            .Returns((5000m, 6000m, 1000m, 20m, new List<InvestmentPerformance>()));

        await _handler.Handle(new GetInvestmentPerformanceQuery(), CancellationToken.None);

        await _reportService.Received(1).CalculateInvestmentPerformanceAsync(UserId, null, null, null, "USD", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsInvestmentPerformanceResponse()
    {
        _reportService
            .CalculateInvestmentPerformanceAsync(UserId, null, null, null, "USD", Arg.Any<CancellationToken>())
            .Returns((5000m, 6000m, 1000m, 20m, new List<InvestmentPerformance>()));

        var result = await _handler.Handle(new GetInvestmentPerformanceQuery(), CancellationToken.None);

        result.TotalInvested.Should().Be(5000m);
        result.CurrentValue.Should().Be(6000m);
        result.TotalGainLoss.Should().Be(1000m);
        result.Currency.Should().Be("USD");
    }
}
