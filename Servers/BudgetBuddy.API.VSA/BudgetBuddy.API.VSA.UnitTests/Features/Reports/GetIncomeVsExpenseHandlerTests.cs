using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Reports.GetIncomeVsExpense;
using BudgetBuddy.API.VSA.Features.Reports.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Reports;

public class GetIncomeVsExpenseHandlerTests
{
    private readonly IReportService _reportService = Substitute.For<IReportService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserCurrencyService _userCurrencyService = Substitute.For<IUserCurrencyService>();
    private readonly GetIncomeVsExpenseHandler _handler;
    private const string UserId = "user-123";

    public GetIncomeVsExpenseHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _userCurrencyService.GetDisplayCurrencyAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns("USD");
        _handler = new GetIncomeVsExpenseHandler(_reportService, _currentUserService, _userCurrencyService, NullLogger<GetIncomeVsExpenseHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CallsReportServiceWithCorrectArgs()
    {
        _reportService
            .CalculateIncomeVsExpenseAsync(UserId, null, null, null, "USD", Arg.Any<CancellationToken>())
            .Returns((2000m, 1000m, 1000m, 5, 10));
        _reportService
            .GetMonthlyBreakdownAsync(UserId, null, null, null, "USD", Arg.Any<CancellationToken>())
            .Returns(new List<MonthlyBreakdown>());

        await _handler.Handle(new GetIncomeVsExpenseQuery(), CancellationToken.None);

        await _reportService.Received(1).CalculateIncomeVsExpenseAsync(UserId, null, null, null, "USD", Arg.Any<CancellationToken>());
        await _reportService.Received(1).GetMonthlyBreakdownAsync(UserId, null, null, null, "USD", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsIncomeVsExpenseResponse()
    {
        _reportService
            .CalculateIncomeVsExpenseAsync(UserId, null, null, null, "USD", Arg.Any<CancellationToken>())
            .Returns((2000m, 1200m, 800m, 8, 12));
        _reportService
            .GetMonthlyBreakdownAsync(UserId, null, null, null, "USD", Arg.Any<CancellationToken>())
            .Returns(new List<MonthlyBreakdown>());

        var result = await _handler.Handle(new GetIncomeVsExpenseQuery(), CancellationToken.None);

        result.TotalIncome.Should().Be(2000m);
        result.TotalExpense.Should().Be(1200m);
        result.NetIncome.Should().Be(800m);
        result.Currency.Should().Be("USD");
    }
}
