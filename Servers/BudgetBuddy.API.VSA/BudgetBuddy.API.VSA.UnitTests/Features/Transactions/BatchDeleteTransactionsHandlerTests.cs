using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.Transactions.BatchDeleteTransactions;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class BatchDeleteTransactionsHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserCacheInvalidator _cacheInvalidator = Substitute.For<IUserCacheInvalidator>();
    private readonly BatchDeleteTransactionsHandler _handler;
    private const string UserId = "user-123";

    public BatchDeleteTransactionsHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        var batchDeleteService = new BatchDeleteService(NullLogger<BatchDeleteService>.Instance);
        _handler = new BatchDeleteTransactionsHandler(_db, _currentUserService, batchDeleteService, _cacheInvalidator);
    }

    [Fact]
    public async Task Handle_WhenNoneOfTheIdsExist_ReturnsAllAsFailed()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var result = await _handler.Handle(new BatchDeleteTransactionsCommand(ids), CancellationToken.None);

        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(2);
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenTransactionsBelongToOtherUser_ReportsAsNotFound()
    {
        var accountId = Guid.NewGuid();
        var txId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = "other-user", Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Transactions.Add(new Transaction { Id = txId, UserId = "other-user", AccountId = accountId, Amount = 50m, CurrencyCode = "USD", TransactionType = TransactionType.Expense, TransactionDate = new LocalDate(2024, 6, 1) });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new BatchDeleteTransactionsCommand(new List<Guid> { txId }), CancellationToken.None);

        result.FailedCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
    }

    public void Dispose() => _db.Dispose();
}
