using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Transactions.BatchUpdateTransactions;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class BatchUpdateTransactionsHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly BatchUpdateTransactionsHandler _handler;
    private const string UserId = "user-123";

    public BatchUpdateTransactionsHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new BatchUpdateTransactionsHandler(_db, _currentUserService, NullLogger<BatchUpdateTransactionsHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotBelongToUser_ReturnsFailureForAll()
    {
        var transactionIds = new List<Guid> { Guid.NewGuid() };
        var nonExistentCategoryId = Guid.NewGuid();

        var result = await _handler.Handle(
            new BatchUpdateTransactionsCommand(transactionIds, nonExistentCategoryId, null),
            CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Contains("Category not found"));
    }

    [Fact]
    public async Task Handle_WhenTransactionIdsDoNotExist_ReportsAllAsFailed()
    {
        var result = await _handler.Handle(
            new BatchUpdateTransactionsCommand(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }, null, "vacation"),
            CancellationToken.None);

        // BulkUpdateAsync is not called when transactions list is empty, so SuccessCount=0
        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithValidCategoryAndNoMatchingTransactions_ReturnsEmptySuccess()
    {
        var categoryId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Food" });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(
            new BatchUpdateTransactionsCommand(new List<Guid> { Guid.NewGuid() }, categoryId, null),
            CancellationToken.None);

        result.TotalRequested.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
    }

    public void Dispose() => _db.Dispose();
}
