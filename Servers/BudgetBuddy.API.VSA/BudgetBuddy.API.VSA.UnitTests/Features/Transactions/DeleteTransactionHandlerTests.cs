using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.Transactions.DeleteTransaction;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class DeleteTransactionHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserCacheInvalidator _cacheInvalidator = Substitute.For<IUserCacheInvalidator>();
    private readonly DeleteTransactionHandler _handler;
    private const string UserId = "user-123";

    public DeleteTransactionHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new DeleteTransactionHandler(_db, _currentUserService, _cacheInvalidator, NullLogger<DeleteTransactionHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenTransactionExists_DeletesIt()
    {
        var accountId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Transactions.Add(new Transaction { Id = transactionId, UserId = UserId, AccountId = accountId, Amount = 50m, CurrencyCode = "USD", TransactionType = TransactionType.Expense, TransactionDate = new LocalDate(2024, 6, 1) });
        await _db.SaveChangesAsync();

        await _handler.Handle(new DeleteTransactionCommand(transactionId), CancellationToken.None);

        _db.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenTransactionNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new DeleteTransactionCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTransactionBelongsToOtherUser_ThrowsNotFoundException()
    {
        var accountId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = "other-user", Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Transactions.Add(new Transaction { Id = transactionId, UserId = "other-user", AccountId = accountId, Amount = 50m, CurrencyCode = "USD", TransactionType = TransactionType.Expense, TransactionDate = new LocalDate(2024, 6, 1) });
        await _db.SaveChangesAsync();

        var act = () => _handler.Handle(new DeleteTransactionCommand(transactionId), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}
