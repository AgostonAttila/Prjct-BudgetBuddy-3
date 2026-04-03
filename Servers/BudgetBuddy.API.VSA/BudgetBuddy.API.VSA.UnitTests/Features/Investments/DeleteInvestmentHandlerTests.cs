using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.Investments.DeleteInvestment;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class DeleteInvestmentHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserCacheInvalidator _cacheInvalidator = Substitute.For<IUserCacheInvalidator>();
    private readonly DeleteInvestmentHandler _handler;
    private const string UserId = "user-123";

    public DeleteInvestmentHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new DeleteInvestmentHandler(_db, _currentUserService, _cacheInvalidator, NullLogger<DeleteInvestmentHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenInvestmentExists_DeletesIt()
    {
        var investmentId = Guid.NewGuid();
        _db.Investments.Add(new Investment { Id = investmentId, UserId = UserId, Symbol = "AAPL", Name = "Apple", Type = InvestmentType.Stock, Quantity = 10, PurchasePrice = 150, CurrencyCode = "USD", PurchaseDate = new LocalDate(2024, 1, 1) });
        await _db.SaveChangesAsync();

        await _handler.Handle(new DeleteInvestmentCommand(investmentId), CancellationToken.None);

        _db.Investments.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenInvestmentNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new DeleteInvestmentCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenInvestmentBelongsToOtherUser_ThrowsNotFoundException()
    {
        var investmentId = Guid.NewGuid();
        _db.Investments.Add(new Investment { Id = investmentId, UserId = "other-user", Symbol = "BTC", Name = "Bitcoin", Type = InvestmentType.Crypto, Quantity = 1, PurchasePrice = 30000, CurrencyCode = "USD", PurchaseDate = new LocalDate(2024, 1, 1) });
        await _db.SaveChangesAsync();

        var act = () => _handler.Handle(new DeleteInvestmentCommand(investmentId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}
