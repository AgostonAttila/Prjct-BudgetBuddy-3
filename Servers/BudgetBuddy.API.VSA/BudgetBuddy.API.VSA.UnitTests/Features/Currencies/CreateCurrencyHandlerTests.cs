using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Features.Currencies.CreateCurrency;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Currencies;

public class CreateCurrencyHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly CreateCurrencyHandler _handler;

    public CreateCurrencyHandlerTests()
    {
        _mapper.Map<Currency>(Arg.Any<CreateCurrencyCommand>()).Returns(callInfo =>
        {
            var cmd = callInfo.ArgAt<CreateCurrencyCommand>(0);
            return new Currency { Code = cmd.Code.ToUpperInvariant(), Symbol = cmd.Symbol, Name = cmd.Name };
        });
        _mapper.Map<CreateCurrencyResponse>(Arg.Any<Currency>()).Returns(callInfo =>
        {
            var c = callInfo.ArgAt<Currency>(0);
            return new CreateCurrencyResponse(c.Id, c.Code, c.Symbol, c.Name);
        });
        _handler = new CreateCurrencyHandler(_db, _mapper, NullLogger<CreateCurrencyHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenCurrencyCodeAlreadyExists_ThrowsInvalidOperationException()
    {
        _db.Currencies.Add(new Currency { Id = Guid.NewGuid(), Code = "USD", Symbol = "$", Name = "US Dollar" });
        await _db.SaveChangesAsync();

        var act = () => _handler.Handle(new CreateCurrencyCommand("USD", "$", "US Dollar"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenCurrencyIsNew_CreatesCurrencyInDatabase()
    {
        await _handler.Handle(new CreateCurrencyCommand("EUR", "€", "Euro"), CancellationToken.None);

        _db.Currencies.Should().HaveCount(1);
        _db.Currencies.Single().Code.Should().Be("EUR");
    }

    [Fact]
    public async Task Handle_ReturnsMappedResponse()
    {
        var result = await _handler.Handle(new CreateCurrencyCommand("GBP", "£", "British Pound"), CancellationToken.None);
        result.Should().NotBeNull();
    }

    public void Dispose() => _db.Dispose();
}
