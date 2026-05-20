using BudgetBuddy.Service.ReferenceData.Messaging.Handlers;
using BudgetBuddy.Service.ReferenceData.ReadModels;
using BudgetBuddy.Shared.Messages.Events.Accounts;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Handlers;

public class ReferenceDataAccountSnapshotHandlerTests
{
    private readonly IAccountSnapshotRepository _repo =
        Substitute.For<IAccountSnapshotRepository>();

    private readonly ILogger<AccountSnapshotHandler> _logger =
        Substitute.For<ILogger<AccountSnapshotHandler>>();

    // ── AccountCreatedEvent ──────────────────────────────────────────────────────

    [Fact]
    public async Task Created_upserts_snapshot_with_IsActive_true()
    {
        var accountId = Guid.NewGuid();
        var evt = new AccountCreatedEvent
        {
            AccountId      = accountId,
            UserId         = "user-1",
            Name           = "Checking",
            Currency       = "HUF",
            InitialBalance = 10_000m,
        };

        await AccountSnapshotHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<AccountSnapshot>(s =>
                s.AccountId == accountId &&
                s.UserId    == "user-1"  &&
                s.Name      == "Checking" &&
                s.Currency  == "HUF"    &&
                s.IsActive),
            CancellationToken.None);
    }

    [Fact]
    public async Task Created_does_not_call_DeleteAsync()
    {
        var evt = new AccountCreatedEvent
        {
            AccountId = Guid.NewGuid(),
            UserId    = "user-1",
            Name      = "Savings",
            Currency  = "EUR",
        };

        await AccountSnapshotHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().DeleteAsync(Guid.Empty, default);
    }

    // ── AccountUpdatedEvent ──────────────────────────────────────────────────────

    [Fact]
    public async Task Updated_upserts_snapshot_with_new_name_and_IsActive_status()
    {
        var accountId = Guid.NewGuid();
        var evt = new AccountUpdatedEvent
        {
            AccountId = accountId,
            UserId    = "user-1",
            Name      = "Renamed",
            Currency  = "USD",
            IsActive  = false,
        };

        await AccountSnapshotHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<AccountSnapshot>(s =>
                s.AccountId == accountId &&
                s.Name      == "Renamed" &&
                !s.IsActive),
            CancellationToken.None);
    }

    [Fact]
    public async Task Updated_preserves_currency_from_event()
    {
        var accountId = Guid.NewGuid();
        var evt = new AccountUpdatedEvent
        {
            AccountId = accountId,
            UserId    = "user-1",
            Name      = "Main",
            Currency  = "CHF",
            IsActive  = true,
        };

        await AccountSnapshotHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<AccountSnapshot>(s => s.Currency == "CHF"),
            CancellationToken.None);
    }

    // ── AccountDeletedEvent ──────────────────────────────────────────────────────

    [Fact]
    public async Task Deleted_calls_DeleteAsync_with_correct_id()
    {
        var accountId = Guid.NewGuid();
        var evt       = new AccountDeletedEvent { AccountId = accountId, UserId = "user-1" };

        await AccountSnapshotHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).DeleteAsync(accountId, CancellationToken.None);
    }

    [Fact]
    public async Task Deleted_does_not_call_UpsertAsync()
    {
        var evt = new AccountDeletedEvent { AccountId = Guid.NewGuid(), UserId = "user-1" };

        await AccountSnapshotHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().UpsertAsync(default!, default);
    }
}
