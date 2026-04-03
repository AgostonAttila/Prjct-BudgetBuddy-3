using MapsterMapper;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Features.Accounts.CreateAccount;
using BudgetBuddy.API.VSA.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Accounts;

public class CreateAccountHandlerTests : IDisposable
{
    private readonly AppDbContext _db = DbContextFactory.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly CreateAccountHandler _handler;
    private const string UserId = "user-123";

    public CreateAccountHandlerTests()
    {
        _currentUserService.GetCurrentUserId().Returns(UserId);
        _mapper.Map<Account>(Arg.Any<CreateAccountCommand>()).Returns(callInfo =>
        {
            var cmd = callInfo.ArgAt<CreateAccountCommand>(0);
            return new Account { Name = cmd.Name, Description = cmd.Description, DefaultCurrencyCode = cmd.DefaultCurrencyCode, InitialBalance = cmd.InitialBalance };
        });
        _mapper.Map<CreateAccountResponse>(Arg.Any<Account>()).Returns(callInfo =>
        {
            var acc = callInfo.ArgAt<Account>(0);
            return new CreateAccountResponse(acc.Id, acc.Name, acc.Description, acc.DefaultCurrencyCode, acc.InitialBalance);
        });
        _handler = new CreateAccountHandler(_db, _mapper, _currentUserService, NullLogger<CreateAccountHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CreatesAccountInDatabase()
    {
        var command = new CreateAccountCommand("Savings", "Main savings", "USD", 1000m);

        await _handler.Handle(command, CancellationToken.None);

        var account = _db.Accounts.Single();
        account.Name.Should().Be("Savings");
        account.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_SetsUserIdFromCurrentUser()
    {
        var command = new CreateAccountCommand("Checking", "", "HUF", 0);

        await _handler.Handle(command, CancellationToken.None);

        _db.Accounts.Single().UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_ReturnsCreatedAccountResponse()
    {
        var command = new CreateAccountCommand("Savings", "", "EUR", 500m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
    }

    public void Dispose() => _db.Dispose();
}
