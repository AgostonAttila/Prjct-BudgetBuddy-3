using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Features.Currencies.GetCurrencies;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Currencies;

public class GetCurrenciesHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly GetCurrenciesHandler _handler;

    public GetCurrenciesHandlerTests()
    {
        _handler = new GetCurrenciesHandler(_db, new PassthroughHybridCache(), NullLogger<GetCurrenciesHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenNoCurrencies_ReturnsEmptyList()
    {
        var result = await _handler.Handle(new GetCurrenciesQuery(), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsAllCurrenciesOrderedByCode()
    {
        _db.Currencies.AddRange(
            new Currency { Id = Guid.NewGuid(), Code = "USD", Symbol = "$", Name = "US Dollar" },
            new Currency { Id = Guid.NewGuid(), Code = "EUR", Symbol = "€", Name = "Euro" },
            new Currency { Id = Guid.NewGuid(), Code = "HUF", Symbol = "Ft", Name = "Hungarian Forint" }
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetCurrenciesQuery(), CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].Code.Should().Be("EUR");
        result[1].Code.Should().Be("HUF");
        result[2].Code.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_ReturnsCurrencyDtoWithCorrectFields()
    {
        _db.Currencies.Add(new Currency { Id = Guid.NewGuid(), Code = "USD", Symbol = "$", Name = "US Dollar" });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetCurrenciesQuery(), CancellationToken.None);

        result[0].Code.Should().Be("USD");
        result[0].Symbol.Should().Be("$");
        result[0].Name.Should().Be("US Dollar");
    }

    public void Dispose() => _db.Dispose();
}
