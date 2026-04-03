using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Features.Currencies.DeleteCurrency;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Currencies;

public class DeleteCurrencyHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly DeleteCurrencyHandler _handler;

    public DeleteCurrencyHandlerTests()
    {
        _handler = new DeleteCurrencyHandler(_db, NullLogger<DeleteCurrencyHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenCurrencyExists_DeletesIt()
    {
        var id = Guid.NewGuid();
        _db.Currencies.Add(new Currency { Id = id, Code = "USD", Symbol = "$", Name = "US Dollar" });
        await _db.SaveChangesAsync();

        await _handler.Handle(new DeleteCurrencyCommand(id), CancellationToken.None);

        _db.Currencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCurrencyNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new DeleteCurrencyCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCurrencyInUseByAccount_ThrowsValidationException()
    {
        var id = Guid.NewGuid();
        _db.Currencies.Add(new Currency { Id = id, Code = "USD", Symbol = "$", Name = "US Dollar" });
        _db.Accounts.Add(new Account { Id = Guid.NewGuid(), UserId = "user-1", Name = "My Account", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        var act = () => _handler.Handle(new DeleteCurrencyCommand(id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose() => _db.Dispose();
}
