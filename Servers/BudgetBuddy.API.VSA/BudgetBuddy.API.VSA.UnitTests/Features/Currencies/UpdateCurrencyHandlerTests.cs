using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Features.Currencies.UpdateCurrency;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Currencies;

public class UpdateCurrencyHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UpdateCurrencyHandler _handler;

    public UpdateCurrencyHandlerTests()
    {
        _mapper.Map<CurrencyResponse>(Arg.Any<Currency>()).Returns(callInfo =>
        {
            var c = callInfo.ArgAt<Currency>(0);
            return new CurrencyResponse(c.Id, c.Code, c.Symbol, c.Name);
        });
        _handler = new UpdateCurrencyHandler(_db, _mapper, NullLogger<UpdateCurrencyHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenCurrencyNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new UpdateCurrencyCommand(Guid.NewGuid(), "EUR", "€", "Euro"), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCurrencyExists_UpdatesFields()
    {
        var id = Guid.NewGuid();
        _db.Currencies.Add(new Currency { Id = id, Code = "USD", Symbol = "$", Name = "US Dollar" });
        await _db.SaveChangesAsync();

        await _handler.Handle(new UpdateCurrencyCommand(id, "USD", "$", "United States Dollar"), CancellationToken.None);

        _db.Currencies.Single().Name.Should().Be("United States Dollar");
    }

    [Fact]
    public async Task Handle_WhenCodeConflictsWithOther_ThrowsInvalidOperationException()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        _db.Currencies.AddRange(
            new Currency { Id = id1, Code = "USD", Symbol = "$", Name = "US Dollar" },
            new Currency { Id = id2, Code = "EUR", Symbol = "€", Name = "Euro" }
        );
        await _db.SaveChangesAsync();

        var act = () => _handler.Handle(new UpdateCurrencyCommand(id1, "EUR", "$", "US Dollar"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose() => _db.Dispose();
}
