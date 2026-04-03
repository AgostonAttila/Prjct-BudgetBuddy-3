using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Investments.Services;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class PortfolioServiceTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly IInvestmentCalculationService _investmentCalculationService = Substitute.For<IInvestmentCalculationService>();
    private readonly ICurrencyConversionService _currencyConversionService = Substitute.For<ICurrencyConversionService>();
    private readonly IAccountBalanceService _accountBalanceService = Substitute.For<IAccountBalanceService>();
    private readonly PortfolioService _service;
    private const string UserId = "user-123";

    public PortfolioServiceTests()
    {
        _accountBalanceService
            .CalculateAccountBalancesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LocalDate?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceResult>());

        _service = new PortfolioService(
            _db,
            _investmentCalculationService,
            _currencyConversionService,
            _accountBalanceService,
            SystemClock.Instance,
            NullLogger<PortfolioService>.Instance);
    }

    [Fact]
    public async Task CalculateInvestmentValuesAsync_WhenNoInvestments_ReturnsEmpty()
    {
        var result = await _service.CalculateInvestmentValuesAsync(UserId, "USD", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculatePortfolioValueAsync_WhenNoData_ReturnZeroTotals()
    {
        var (accountBalance, investmentValue, accountBreakdown, investmentBreakdown) =
            await _service.CalculatePortfolioValueAsync(UserId, "USD", CancellationToken.None);

        accountBalance.Should().Be(0m);
        investmentValue.Should().Be(0m);
        accountBreakdown.Should().BeEmpty();
        investmentBreakdown.Should().BeEmpty();
    }

    public void Dispose() => _db.Dispose();
}
