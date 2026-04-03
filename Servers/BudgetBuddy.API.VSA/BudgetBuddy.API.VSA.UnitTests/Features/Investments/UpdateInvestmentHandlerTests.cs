using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.Investments.UpdateInvestment;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class UpdateInvestmentHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IUserCacheInvalidator _cacheInvalidator = Substitute.For<IUserCacheInvalidator>();
    private readonly UpdateInvestmentHandler _handler;
    private const string UserId = "user-123";

    public UpdateInvestmentHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _mapper.Map(Arg.Any<UpdateInvestmentCommand>(), Arg.Any<Investment>());
        _mapper.Map<InvestmentResponse>(Arg.Any<Investment>()).Returns(callInfo =>
        {
            var inv = callInfo.ArgAt<Investment>(0);
            return new InvestmentResponse(inv.Id, inv.Symbol, inv.Name, inv.Type, inv.Quantity, inv.PurchasePrice, inv.CurrencyCode, inv.PurchaseDate, inv.Note, inv.AccountId);
        });
        _handler = new UpdateInvestmentHandler(_db, _mapper, _currentUserService, _cacheInvalidator, NullLogger<UpdateInvestmentHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenInvestmentNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateInvestmentCommand(Guid.NewGuid(), "AAPL", "Apple", InvestmentType.Stock, 10, 150, "USD", new LocalDate(2024, 1, 1), null, null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenInvestmentBelongsToOtherUser_ThrowsNotFoundException()
    {
        var investmentId = Guid.NewGuid();
        _db.Investments.Add(new Investment { Id = investmentId, UserId = "other-user", Symbol = "BTC", Name = "Bitcoin", Type = InvestmentType.Crypto, Quantity = 1, PurchasePrice = 30000, CurrencyCode = "USD", PurchaseDate = new LocalDate(2024, 1, 1) });
        await _db.SaveChangesAsync();

        var command = new UpdateInvestmentCommand(investmentId, "BTC", "Bitcoin", InvestmentType.Crypto, 2, 35000, "USD", new LocalDate(2024, 1, 1), null, null);
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenInvestmentExists_CallsMapper()
    {
        var investmentId = Guid.NewGuid();
        _db.Investments.Add(new Investment { Id = investmentId, UserId = UserId, Symbol = "AAPL", Name = "Apple", Type = InvestmentType.Stock, Quantity = 10, PurchasePrice = 150, CurrencyCode = "USD", PurchaseDate = new LocalDate(2024, 1, 1) });
        await _db.SaveChangesAsync();

        var command = new UpdateInvestmentCommand(investmentId, "AAPL", "Apple Inc", InvestmentType.Stock, 15, 160, "USD", new LocalDate(2024, 1, 1), null, null);
        await _handler.Handle(command, CancellationToken.None);

        _mapper.Received(1).Map(Arg.Any<UpdateInvestmentCommand>(), Arg.Any<Investment>());
    }

    public void Dispose() => _db.Dispose();
}
