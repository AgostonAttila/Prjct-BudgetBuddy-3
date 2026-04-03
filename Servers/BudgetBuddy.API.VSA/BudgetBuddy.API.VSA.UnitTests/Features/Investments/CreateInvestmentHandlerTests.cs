using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.Investments.CreateInvestment;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class CreateInvestmentHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IUserCacheInvalidator _cacheInvalidator = Substitute.For<IUserCacheInvalidator>();
    private readonly CreateInvestmentHandler _handler;
    private const string UserId = "user-123";

    public CreateInvestmentHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);

        _mapper.Map<Investment>(Arg.Any<CreateInvestmentCommand>()).Returns(callInfo =>
        {
            var cmd = callInfo.ArgAt<CreateInvestmentCommand>(0);
            return new Investment
            {
                Symbol = cmd.Symbol,
                Name = cmd.Name,
                Type = cmd.Type,
                Quantity = cmd.Quantity,
                PurchasePrice = cmd.PurchasePrice,
                CurrencyCode = cmd.CurrencyCode,
                PurchaseDate = cmd.PurchaseDate
            };
        });

        _mapper.Map<CreateInvestmentResponse>(Arg.Any<Investment>()).Returns(callInfo =>
        {
            var inv = callInfo.ArgAt<Investment>(0);
            return new CreateInvestmentResponse(inv.Id, inv.Symbol, inv.Name, inv.Type, inv.Quantity, inv.PurchasePrice, inv.CurrencyCode, inv.PurchaseDate);
        });

        _handler = new CreateInvestmentHandler(_db, _mapper, _currentUserService, _cacheInvalidator, NullLogger<CreateInvestmentHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CreatesInvestmentInDatabase()
    {
        var command = new CreateInvestmentCommand("AAPL", "Apple Inc", InvestmentType.Stock, 10m, 150m, "USD", new LocalDate(2024, 6, 15), null, null);

        await _handler.Handle(command, CancellationToken.None);

        var investment = _db.Investments.Single();
        investment.Symbol.Should().Be("AAPL");
        investment.Name.Should().Be("Apple Inc");
    }

    [Fact]
    public async Task Handle_SetsUserIdFromCurrentUser()
    {
        var command = new CreateInvestmentCommand("BTC", "Bitcoin", InvestmentType.Crypto, 0.5m, 30000m, "USD", new LocalDate(2024, 1, 1), null, null);

        await _handler.Handle(command, CancellationToken.None);

        _db.Investments.Single().UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_ReturnsMappedResponse()
    {
        var command = new CreateInvestmentCommand("VOO", "Vanguard S&P 500", InvestmentType.Etf, 5m, 400m, "USD", new LocalDate(2024, 3, 10), null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Symbol.Should().Be("VOO");
    }

    public void Dispose() => _db.Dispose();
}
