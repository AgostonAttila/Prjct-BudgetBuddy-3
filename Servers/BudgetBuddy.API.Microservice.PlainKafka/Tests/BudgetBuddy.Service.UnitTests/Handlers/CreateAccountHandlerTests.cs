using BudgetBuddy.Service.Accounts.Domain;
using BudgetBuddy.Service.Accounts.Features.CreateAccount;
using BudgetBuddy.Service.Accounts.Persistence;
using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using FluentAssertions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Handlers;

public sealed class CreateAccountHandlerTests : IDisposable
{
    private readonly AccountsDbContext _db;
    private readonly DomainEventCollector _collector;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IEventPublisher _publisher;
    private readonly CreateAccountHandler _sut;

    private const string TestUserId = "user-abc";

    public CreateAccountHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db          = new AccountsDbContext(options);
        _collector   = new DomainEventCollector();
        _mapper      = Substitute.For<IMapper>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _publisher   = Substitute.For<IEventPublisher>();

        _currentUser.GetCurrentUserId().Returns(TestUserId);

        _sut = new CreateAccountHandler(
            _db, _mapper, _currentUser, _publisher, _collector,
            NullLogger<CreateAccountHandler>.Instance);
    }

    [Fact]
    public async Task Handle_saves_account_to_database()
    {
        var cmd = new CreateAccountCommand("My Account", "desc", "HUF", 50000m);
        SetupMapper(cmd);

        await _sut.Handle(cmd, default);

        (await _db.Accounts.CountAsync()).Should().Be(1);
        var saved = await _db.Accounts.SingleAsync();
        saved.Name.Should().Be("My Account");
        saved.DefaultCurrencyCode.Should().Be("HUF");
        saved.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task Handle_collects_AccountCreatedEvent()
    {
        var cmd = new CreateAccountCommand("My Account", "desc", "HUF", 50000m);
        SetupMapper(cmd);

        await _sut.Handle(cmd, default);

        var events = _collector.GetAndClear();
        events.Should().HaveCount(1);
        var ev = events[0].Should().BeOfType<AccountCreatedEvent>().Subject;
        ev.Name.Should().Be("My Account");
        ev.Currency.Should().Be("HUF");
        ev.InitialBalance.Should().Be(50000m);
        ev.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task Handle_publishes_state_snapshot_to_changelog()
    {
        var cmd = new CreateAccountCommand("My Account", "desc", "HUF", 0m);
        SetupMapper(cmd);

        await _sut.Handle(cmd, default);

        await _publisher.Received(1).PublishStateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_mapped_response()
    {
        var cmd = new CreateAccountCommand("My Account", "desc", "USD", 1000m);
        var expectedResponse = SetupMapper(cmd);

        var result = await _sut.Handle(cmd, default);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private CreateAccountResponse SetupMapper(CreateAccountCommand cmd)
    {
        var account = new Account
        {
            Id                   = Guid.NewGuid(),
            Name                 = cmd.Name,
            Description          = cmd.Description,
            DefaultCurrencyCode  = cmd.DefaultCurrencyCode,
            InitialBalance       = cmd.InitialBalance,
        };
        var response = new CreateAccountResponse(
            account.Id, account.Name, account.Description,
            account.DefaultCurrencyCode, account.InitialBalance);

        _mapper.Map<Account>(Arg.Any<object>()).Returns(account);
        _mapper.Map<CreateAccountResponse>(Arg.Any<object>()).Returns(response);
        return response;
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
