using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Shared.Services;
using BudgetBuddy.API.VSA.Features.Transactions.CreateTransaction;
using BudgetBuddy.API.VSA.Features.Transactions.Services;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class CreateTransactionHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ITransactionValidationService _validationService = Substitute.For<ITransactionValidationService>();
    private readonly IUserCacheInvalidator _cacheInvalidator = Substitute.For<IUserCacheInvalidator>();
    private readonly CreateTransactionHandler _handler;
    private const string UserId = "user-123";
    private static readonly LocalDate TestDate = new(2024, 6, 15);

    public CreateTransactionHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _mapper.Map<Transaction>(Arg.Any<CreateTransactionCommand>()).Returns(callInfo =>
        {
            var cmd = callInfo.ArgAt<CreateTransactionCommand>(0);
            return new Transaction { AccountId = cmd.AccountId, Amount = cmd.Amount, CurrencyCode = cmd.CurrencyCode, TransactionType = cmd.TransactionType, TransactionDate = cmd.TransactionDate, IsTransfer = cmd.IsTransfer };
        });
        _mapper.Map<TransactionResponse>(Arg.Any<Transaction>()).Returns(callInfo =>
        {
            var t = callInfo.ArgAt<Transaction>(0);
            return new TransactionResponse(t.Id, t.AccountId, null, null, t.Amount, t.CurrencyCode, null, t.TransactionType, t.PaymentType, null, t.TransactionDate, t.IsTransfer, null, null, null);
        });
        _handler = new CreateTransactionHandler(_db, _mapper, _currentUserService, _validationService, _cacheInvalidator, NullLogger<CreateTransactionHandler>.Instance);
    }

    private static CreateTransactionCommand MakeCommand(Guid accountId, bool isTransfer = false, Guid? transferTo = null)
        => new(accountId, null, null, 100m, "USD", null, TransactionType.Expense, PaymentType.Cash, null, TestDate, isTransfer, transferTo, null, null);

    [Fact]
    public async Task Handle_WhenAccountNotFound_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _validationService
            .When(x => x.ValidateAccountOwnershipAsync(missingId, UserId, Arg.Any<CancellationToken>()))
            .Do(_ => throw new NotFoundException("Account", missingId));

        var act = () => _handler.Handle(MakeCommand(missingId), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAccountBelongsToOtherUser_ThrowsNotFoundException()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = "other-user", Name = "Test", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();
        _validationService
            .When(x => x.ValidateAccountOwnershipAsync(accountId, UserId, Arg.Any<CancellationToken>()))
            .Do(_ => throw new NotFoundException("Account", accountId));

        var act = () => _handler.Handle(MakeCommand(accountId), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAccountExists_CreatesTransaction()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Test", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        await _handler.Handle(MakeCommand(accountId), CancellationToken.None);

        _db.Transactions.Should().HaveCount(1);
        _db.Transactions.Single().UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_WhenTransferWithMissingDestinationAccount_ThrowsNotFoundException()
    {
        var accountId = Guid.NewGuid();
        var missingDestId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Source", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();
        _validationService
            .When(x => x.ValidateTransferDestinationAsync(missingDestId, UserId, Arg.Any<CancellationToken>()))
            .Do(_ => throw new NotFoundException("Account", missingDestId));

        var act = () => _handler.Handle(MakeCommand(accountId, isTransfer: true, transferTo: missingDestId), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}
