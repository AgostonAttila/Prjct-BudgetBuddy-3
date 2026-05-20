using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using BudgetBuddy.Shared.Messages.Events.Accounts;

// Each service has its own AccountSnapshotHandler in its own namespace.
// Aliases prevent ambiguity.
using TransactionsHandler   = BudgetBuddy.Service.Transactions.Messaging.Handlers.AccountSnapshotHandler;
using AnalyticsHandler      = BudgetBuddy.Service.Analytics.Messaging.Handlers.AccountSnapshotHandler;
using InvestmentsHandler    = BudgetBuddy.Service.Investments.Messaging.Handlers.AccountSnapshotHandler;

using ITransactionsRepo     = BudgetBuddy.Service.Transactions.ReadModels.IAccountSnapshotRepository;
using IAnalyticsRepo        = BudgetBuddy.Service.Analytics.ReadModels.IAccountSnapshotRepository;
using IInvestmentsRepo      = BudgetBuddy.Service.Investments.ReadModels.IAccountSnapshotRepository;

using TransactionsSnapshot  = BudgetBuddy.Service.Transactions.ReadModels.AccountSnapshot;
using AnalyticsSnapshot     = BudgetBuddy.Service.Analytics.ReadModels.AccountSnapshot;
using InvestmentsSnapshot   = BudgetBuddy.Service.Investments.ReadModels.AccountSnapshot;

using ITransactionsCache    = BudgetBuddy.Service.Transactions.ReadModels.IAccountSnapshotService;

namespace BudgetBuddy.Service.UnitTests.Handlers;

// ════════════════════════════════════════════════════════════════════════════════
// Transactions service — AccountSnapshotHandler
// (has L1+L2 cache via IAccountSnapshotService)
// ════════════════════════════════════════════════════════════════════════════════
public class TransactionsAccountSnapshotHandlerTests
{
    private readonly ITransactionsRepo  _repo  = Substitute.For<ITransactionsRepo>();
    private readonly ITransactionsCache _cache = Substitute.For<ITransactionsCache>();
    private readonly ILogger<TransactionsHandler> _logger = Substitute.For<ILogger<TransactionsHandler>>();

    [Fact]
    public async Task Created_upserts_snapshot_and_populates_cache()
    {
        var evt = new AccountCreatedEvent
        {
            AccountId      = Guid.NewGuid(),
            UserId         = "user-1",
            Name           = "Main Account",
            Currency       = "HUF",
            InitialBalance = 500m,
        };

        await TransactionsHandler.Handle(evt, _repo, _cache, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<TransactionsSnapshot>(s =>
                s.AccountId == evt.AccountId &&
                s.UserId    == evt.UserId    &&
                s.Name      == evt.Name      &&
                s.Currency  == evt.Currency  &&
                s.IsActive),
            CancellationToken.None);

        await _cache.Received(1).PopulateAsync(Arg.Any<TransactionsSnapshot>(), CancellationToken.None);
    }

    [Fact]
    public async Task Updated_upserts_snapshot_with_new_values_and_populates_cache()
    {
        var evt = new AccountUpdatedEvent
        {
            AccountId = Guid.NewGuid(),
            UserId    = "user-1",
            Name      = "Renamed",
            Currency  = "EUR",
            IsActive  = false,
        };

        await TransactionsHandler.Handle(evt, _repo, _cache, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<TransactionsSnapshot>(s =>
                s.AccountId == evt.AccountId &&
                s.Name      == "Renamed"     &&
                !s.IsActive),
            CancellationToken.None);

        await _cache.Received(1).PopulateAsync(Arg.Any<TransactionsSnapshot>(), CancellationToken.None);
    }

    [Fact]
    public async Task Deleted_removes_from_repo_and_invalidates_cache()
    {
        var evt = new AccountDeletedEvent { AccountId = Guid.NewGuid(), UserId = "user-1" };

        await TransactionsHandler.Handle(evt, _repo, _cache, _logger, CancellationToken.None);

        await _repo.Received(1).DeleteAsync(evt.AccountId, CancellationToken.None);
        await _cache.Received(1).InvalidateAsync(evt.AccountId, CancellationToken.None);
    }
}

// ════════════════════════════════════════════════════════════════════════════════
// Analytics service — AccountSnapshotHandler
// (sets Balance from InitialBalance; preserves Balance on update)
// ════════════════════════════════════════════════════════════════════════════════
public class AnalyticsAccountSnapshotHandlerTests
{
    private readonly IAnalyticsRepo _repo   = Substitute.For<IAnalyticsRepo>();
    private readonly ILogger<AnalyticsHandler> _logger = Substitute.For<ILogger<AnalyticsHandler>>();

    [Fact]
    public async Task Created_sets_balance_from_InitialBalance()
    {
        var evt = new AccountCreatedEvent
        {
            AccountId      = Guid.NewGuid(),
            UserId         = "user-1",
            Name           = "Savings",
            Currency       = "USD",
            InitialBalance = 1_000m,
        };

        await AnalyticsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<AnalyticsSnapshot>(s =>
                s.AccountId == evt.AccountId &&
                s.Balance   == 1_000m        &&
                s.IsActive),
            CancellationToken.None);
    }

    [Fact]
    public async Task Updated_preserves_existing_balance_when_snapshot_exists()
    {
        var accountId = Guid.NewGuid();
        var existing  = new AnalyticsSnapshot { AccountId = accountId, Balance = 2_500m };
        _repo.FindAsync(accountId, CancellationToken.None).Returns(existing);

        var evt = new AccountUpdatedEvent
        {
            AccountId = accountId,
            UserId    = "user-1",
            Name      = "Updated Name",
            Currency  = "USD",
            IsActive  = true,
        };

        await AnalyticsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<AnalyticsSnapshot>(s => s.Balance == 2_500m),
            CancellationToken.None);
    }

    [Fact]
    public async Task Updated_uses_zero_balance_when_no_existing_snapshot()
    {
        var accountId = Guid.NewGuid();
        _repo.FindAsync(accountId, CancellationToken.None).Returns((AnalyticsSnapshot?)null);

        var evt = new AccountUpdatedEvent
        {
            AccountId = accountId,
            UserId    = "user-1",
            Name      = "New",
            Currency  = "HUF",
            IsActive  = true,
        };

        await AnalyticsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<AnalyticsSnapshot>(s => s.Balance == 0m),
            CancellationToken.None);
    }

    [Fact]
    public async Task Deleted_removes_snapshot()
    {
        var evt = new AccountDeletedEvent { AccountId = Guid.NewGuid(), UserId = "user-1" };

        await AnalyticsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).DeleteAsync(evt.AccountId, CancellationToken.None);
    }
}

// ════════════════════════════════════════════════════════════════════════════════
// Investments service — AccountSnapshotHandler
// (same balance logic as Analytics)
// ════════════════════════════════════════════════════════════════════════════════
public class InvestmentsAccountSnapshotHandlerTests
{
    private readonly IInvestmentsRepo _repo   = Substitute.For<IInvestmentsRepo>();
    private readonly ILogger<InvestmentsHandler> _logger = Substitute.For<ILogger<InvestmentsHandler>>();

    [Fact]
    public async Task Created_sets_balance_from_InitialBalance()
    {
        var evt = new AccountCreatedEvent
        {
            AccountId      = Guid.NewGuid(),
            UserId         = "user-1",
            Name           = "Brokerage",
            Currency       = "USD",
            InitialBalance = 5_000m,
        };

        await InvestmentsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<InvestmentsSnapshot>(s =>
                s.AccountId == evt.AccountId &&
                s.Balance   == 5_000m        &&
                s.IsActive),
            CancellationToken.None);
    }

    [Fact]
    public async Task Updated_preserves_existing_balance()
    {
        var accountId = Guid.NewGuid();
        _repo.FindAsync(accountId, CancellationToken.None)
             .Returns(new InvestmentsSnapshot { AccountId = accountId, Balance = 7_000m });

        var evt = new AccountUpdatedEvent
        {
            AccountId = accountId,
            UserId    = "user-1",
            Name      = "Renamed",
            Currency  = "USD",
            IsActive  = true,
        };

        await InvestmentsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<InvestmentsSnapshot>(s => s.Balance == 7_000m),
            CancellationToken.None);
    }

    [Fact]
    public async Task Updated_uses_zero_balance_when_no_existing_snapshot()
    {
        var accountId = Guid.NewGuid();
        _repo.FindAsync(accountId, CancellationToken.None).Returns((InvestmentsSnapshot?)null);

        var evt = new AccountUpdatedEvent
        {
            AccountId = accountId,
            UserId    = "user-1",
            Name      = "New",
            Currency  = "EUR",
            IsActive  = true,
        };

        await InvestmentsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<InvestmentsSnapshot>(s => s.Balance == 0m),
            CancellationToken.None);
    }

    [Fact]
    public async Task Deleted_removes_snapshot()
    {
        var evt = new AccountDeletedEvent { AccountId = Guid.NewGuid(), UserId = "user-1" };

        await InvestmentsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).DeleteAsync(evt.AccountId, CancellationToken.None);
    }
}
