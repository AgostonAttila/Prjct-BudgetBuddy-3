using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Investments.GetInvestments;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class GetInvestmentsHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetInvestmentsHandler _handler;
    private const string UserId = "user-123";

    public GetInvestmentsHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new GetInvestmentsHandler(_db, _currentUserService, NullLogger<GetInvestmentsHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenNoInvestments_ReturnsEmpty()
    {
        var result = await _handler.Handle(new GetInvestmentsQuery(), CancellationToken.None);

        result.Investments.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCurrentUsersInvestments()
    {
        _db.Investments.Add(new Investment { Id = Guid.NewGuid(), UserId = UserId, Symbol = "AAPL", Name = "Apple", Type = InvestmentType.Stock, Quantity = 10, PurchasePrice = 150, CurrencyCode = "USD", PurchaseDate = new LocalDate(2024, 1, 1) });
        _db.Investments.Add(new Investment { Id = Guid.NewGuid(), UserId = "other-user", Symbol = "BTC", Name = "Bitcoin", Type = InvestmentType.Crypto, Quantity = 1, PurchasePrice = 30000, CurrencyCode = "USD", PurchaseDate = new LocalDate(2024, 1, 1) });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetInvestmentsQuery(), CancellationToken.None);

        result.Investments.Should().HaveCount(1);
        result.Investments.Single().Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task Handle_FiltersByType()
    {
        _db.Investments.Add(new Investment { Id = Guid.NewGuid(), UserId = UserId, Symbol = "AAPL", Name = "Apple", Type = InvestmentType.Stock, Quantity = 10, PurchasePrice = 150, CurrencyCode = "USD", PurchaseDate = new LocalDate(2024, 1, 1) });
        _db.Investments.Add(new Investment { Id = Guid.NewGuid(), UserId = UserId, Symbol = "BTC", Name = "Bitcoin", Type = InvestmentType.Crypto, Quantity = 1, PurchasePrice = 30000, CurrencyCode = "USD", PurchaseDate = new LocalDate(2024, 1, 1) });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetInvestmentsQuery(Type: InvestmentType.Stock), CancellationToken.None);

        result.Investments.Should().HaveCount(1);
        result.Investments.Single().Symbol.Should().Be("AAPL");
    }

    public void Dispose() => _db.Dispose();
}
