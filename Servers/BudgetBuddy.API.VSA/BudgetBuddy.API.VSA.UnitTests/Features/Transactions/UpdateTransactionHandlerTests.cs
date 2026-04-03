using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.Transactions.UpdateTransaction;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class UpdateTransactionHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IUserCacheInvalidator _cacheInvalidator = Substitute.For<IUserCacheInvalidator>();
    private readonly UpdateTransactionHandler _handler;
    private const string UserId = "user-123";

    public UpdateTransactionHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _mapper.Map<TransactionResponse>(Arg.Any<Transaction>()).Returns(callInfo =>
        {
            var t = callInfo.ArgAt<Transaction>(0);
            return new TransactionResponse(t.Id, t.AccountId, null, null, t.Amount, t.CurrencyCode, null, t.TransactionType, t.PaymentType, null, t.TransactionDate, null, null);
        });
        _handler = new UpdateTransactionHandler(_db, _mapper, _currentUserService, _cacheInvalidator, NullLogger<UpdateTransactionHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenTransactionNotFound_ThrowsNotFoundException()
    {
        var cmd = new UpdateTransactionCommand(Guid.NewGuid(), null, null, 100m, "USD", null, TransactionType.Expense, PaymentType.Cash, null, new LocalDate(2024, 6, 1), null, null);
        var act = () => _handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTransactionExists_CallsMapperAndSaves()
    {
        var accountId = Guid.NewGuid();
        var txId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });
        _db.Transactions.Add(new Transaction { Id = txId, UserId = UserId, AccountId = accountId, Amount = 50m, CurrencyCode = "USD", TransactionType = TransactionType.Expense, TransactionDate = new LocalDate(2024, 6, 1) });
        await _db.SaveChangesAsync();

        var cmd = new UpdateTransactionCommand(txId, null, null, 200m, "EUR", null, TransactionType.Income, PaymentType.Card, null, new LocalDate(2024, 6, 15), null, null);
        await _handler.Handle(cmd, CancellationToken.None);

        _mapper.Received(1).Map(Arg.Any<UpdateTransactionCommand>(), Arg.Any<Transaction>());
    }

    public void Dispose() => _db.Dispose();
}
