using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Accounts.GetAccountBalance;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Accounts;

public class GetAccountBalanceHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetAccountBalanceHandler _handler;
    private const string UserId = "user-123";

    public GetAccountBalanceHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _handler = new GetAccountBalanceHandler(_db, _currentUserService, NullLogger<GetAccountBalanceHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new GetAccountBalanceQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithNoTransactions_ReturnsInitialBalance()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD", InitialBalance = 500m });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAccountBalanceQuery(accountId), CancellationToken.None);

        result.CurrentBalance.Should().Be(500m);
        result.TotalIncome.Should().Be(0);
        result.TotalExpense.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithIncomeAndExpense_CalculatesCorrectBalance()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD", InitialBalance = 1000m });
        _db.Transactions.AddRange(
            new Transaction { Id = Guid.NewGuid(), AccountId = accountId, UserId = UserId, Amount = 500m, TransactionType = TransactionType.Income, CurrencyCode = "USD", TransactionDate = new LocalDate(2024, 1, 10) },
            new Transaction { Id = Guid.NewGuid(), AccountId = accountId, UserId = UserId, Amount = 200m, TransactionType = TransactionType.Expense, CurrencyCode = "USD", TransactionDate = new LocalDate(2024, 1, 15) }
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAccountBalanceQuery(accountId), CancellationToken.None);

        result.CurrentBalance.Should().Be(1300m); // 1000 + 500 - 200
        result.TotalIncome.Should().Be(500m);
        result.TotalExpense.Should().Be(200m);
        result.TransactionCount.Should().Be(2);
    }

    public void Dispose() => _db.Dispose();
}
