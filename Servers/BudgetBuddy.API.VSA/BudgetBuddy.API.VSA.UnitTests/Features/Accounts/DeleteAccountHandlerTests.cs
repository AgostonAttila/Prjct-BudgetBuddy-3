using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Accounts.DeleteAccount;
using BudgetBuddy.API.VSA.UnitTests.Common;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Accounts;

public class DeleteAccountHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly DeleteAccountHandler _handler;
    private const string UserId = "user-123";

    public DeleteAccountHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new DeleteAccountHandler(_db, _currentUserService, NullLogger<DeleteAccountHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenAccountExists_DeletesIt()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        await _handler.Handle(new DeleteAccountCommand(accountId), CancellationToken.None);

        _db.Accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new DeleteAccountCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAccountBelongsToOtherUser_ThrowsNotFoundException()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = "other-user", Name = "Test", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        var act = () => _handler.Handle(new DeleteAccountCommand(accountId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}
