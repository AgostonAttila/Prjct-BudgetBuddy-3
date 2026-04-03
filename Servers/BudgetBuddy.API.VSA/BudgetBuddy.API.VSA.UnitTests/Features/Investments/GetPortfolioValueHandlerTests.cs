using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Investments.GetPortfolioValue;
using BudgetBuddy.API.VSA.Features.Investments.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class GetPortfolioValueHandlerTests
{
    private readonly IPortfolioService _portfolioService = Substitute.For<IPortfolioService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserCurrencyService _userCurrencyService = Substitute.For<IUserCurrencyService>();
    private readonly GetPortfolioValueHandler _handler;
    private const string UserId = "user-123";

    public GetPortfolioValueHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _userCurrencyService.GetDisplayCurrencyAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns("USD");
        _handler = new GetPortfolioValueHandler(_portfolioService, _currentUserService, _userCurrencyService, NullLogger<GetPortfolioValueHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ReturnsPortfolioResponseWithCorrectValues()
    {
        var accountBreakdown = new List<AccountBalanceDto> { new("Savings", "USD", 1000m, 1000m) };
        var investmentBreakdown = new List<InvestmentValueDto> { new("AAPL", "Apple", 10m, 150m, 180m, 1800m, 300m, 20m) };

        _portfolioService.CalculatePortfolioValueAsync(UserId, "USD", Arg.Any<CancellationToken>())
            .Returns((1000m, 1800m, accountBreakdown, investmentBreakdown));

        var result = await _handler.Handle(new GetPortfolioValueQuery("USD"), CancellationToken.None);

        result.TotalAccountBalance.Should().Be(1000m);
        result.TotalInvestmentValue.Should().Be(1800m);
        result.TotalPortfolioValue.Should().Be(2800m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_WhenNoHoldings_ReturnsZeroTotals()
    {
        _portfolioService.CalculatePortfolioValueAsync(UserId, "USD", Arg.Any<CancellationToken>())
            .Returns((0m, 0m, new List<AccountBalanceDto>(), new List<InvestmentValueDto>()));

        var result = await _handler.Handle(new GetPortfolioValueQuery("USD"), CancellationToken.None);

        result.TotalPortfolioValue.Should().Be(0m);
        result.AccountBreakdown.Should().BeEmpty();
        result.InvestmentBreakdown.Should().BeEmpty();
    }
}
