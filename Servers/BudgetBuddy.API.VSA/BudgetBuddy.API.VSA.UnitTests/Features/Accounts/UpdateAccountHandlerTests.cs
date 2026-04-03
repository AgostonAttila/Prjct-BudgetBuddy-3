using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Exceptions;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Accounts.UpdateAccount;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Accounts;

public class UpdateAccountHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UpdateAccountHandler _handler;
    private const string UserId = "user-123";

    public UpdateAccountHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _mapper.Map(Arg.Any<UpdateAccountCommand>(), Arg.Any<Account>());
        _mapper.Map<UpdateAccountResponse>(Arg.Any<Account>()).Returns(callInfo =>
        {
            var acc = callInfo.ArgAt<Account>(0);
            return new UpdateAccountResponse(acc.Id, acc.Name, acc.Description, acc.DefaultCurrencyCode, acc.InitialBalance);
        });
        _handler = new UpdateAccountHandler(_db, _mapper, _currentUserService, NullLogger<UpdateAccountHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new UpdateAccountCommand(Guid.NewGuid(), "New Name", "", "USD", 0), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAccountBelongsToOtherUser_ThrowsNotFoundException()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = "other-user", Name = "Test", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        var act = () => _handler.Handle(new UpdateAccountCommand(accountId, "New Name", "", "EUR", 500), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAccountExists_CallsMapper()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { Id = accountId, UserId = UserId, Name = "Original", DefaultCurrencyCode = "USD" });
        await _db.SaveChangesAsync();

        await _handler.Handle(new UpdateAccountCommand(accountId, "Updated", "", "EUR", 200), CancellationToken.None);

        _mapper.Received(1).Map(Arg.Any<UpdateAccountCommand>(), Arg.Any<Account>());
    }

    public void Dispose() => _db.Dispose();
}
