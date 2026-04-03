using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Transactions.GetTransactions;
using BudgetBuddy.API.VSA.Features.Transactions.Services;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class GetTransactionsHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ITransactionSearchService _searchService = Substitute.For<ITransactionSearchService>();
    private readonly GetTransactionsHandler _handler;
    private const string UserId = "user-123";

    public GetTransactionsHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _searchService.ApplySearch(Arg.Any<IQueryable<Transaction>>(), Arg.Any<string>())
            .Returns(ci => ci.ArgAt<IQueryable<Transaction>>(0));
        _handler = new GetTransactionsHandler(_db, _currentUserService, _searchService, NullLogger<GetTransactionsHandler>.Instance);
    }

    private async Task<Guid> AddAccountAndTransaction(string userId, decimal amount, TransactionType type, LocalDate date)
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = userId, Name = "Test Account", DefaultCurrencyCode = "USD" });
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), UserId = userId, AccountId = accountId,
            Amount = amount, CurrencyCode = "USD", TransactionType = type,
            TransactionDate = date
        });
        await _db.SaveChangesAsync();
        return accountId;
    }

    [Fact]
    public async Task Handle_WhenNoTransactions_ReturnsEmptyResponse()
    {
        var result = await _handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        result.Transactions.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCurrentUserTransactions()
    {
        await AddAccountAndTransaction(UserId, 100m, TransactionType.Expense, new LocalDate(2024, 6, 1));
        await AddAccountAndTransaction("other-user", 200m, TransactionType.Income, new LocalDate(2024, 6, 2));

        var result = await _handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Transactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_FiltersByAccountId()
    {
        var accountId = await AddAccountAndTransaction(UserId, 100m, TransactionType.Expense, new LocalDate(2024, 6, 1));
        await AddAccountAndTransaction(UserId, 200m, TransactionType.Income, new LocalDate(2024, 6, 2));

        var result = await _handler.Handle(new GetTransactionsQuery(AccountId: accountId), CancellationToken.None);

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_FiltersByTransactionType()
    {
        await AddAccountAndTransaction(UserId, 100m, TransactionType.Expense, new LocalDate(2024, 6, 1));
        await AddAccountAndTransaction(UserId, 200m, TransactionType.Income, new LocalDate(2024, 6, 2));

        var result = await _handler.Handle(new GetTransactionsQuery(Type: TransactionType.Income), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Transactions[0].TransactionType.Should().Be(TransactionType.Income);
    }

    [Fact]
    public async Task Handle_RespectsPageSize()
    {
        for (int i = 0; i < 5; i++)
            await AddAccountAndTransaction(UserId, 10m * (i + 1), TransactionType.Expense, new LocalDate(2024, 6, i + 1));

        var result = await _handler.Handle(new GetTransactionsQuery(PageSize: 2), CancellationToken.None);

        result.Transactions.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
    }

    public void Dispose() => _db.Dispose();
}
