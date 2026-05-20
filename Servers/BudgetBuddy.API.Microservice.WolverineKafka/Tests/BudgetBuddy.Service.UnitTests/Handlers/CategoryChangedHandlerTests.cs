using BudgetBuddy.Shared.Messages.Events.ReferenceData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

// Each service has its own CategoryChangedHandler in its own namespace.
using TransactionsHandler = BudgetBuddy.Service.Transactions.Messaging.Handlers.CategoryChangedHandler;
using BudgetsHandler      = BudgetBuddy.Service.Budgets.Messaging.Handlers.CategoryChangedHandler;
using AnalyticsHandler    = BudgetBuddy.Service.Analytics.Messaging.Handlers.CategoryChangedHandler;

using ITransactionsRepo   = BudgetBuddy.Service.Transactions.ReadModels.ICategorySnapshotRepository;
using IBudgetsRepo        = BudgetBuddy.Service.Budgets.ReadModels.ICategorySnapshotRepository;
using IAnalyticsRepo      = BudgetBuddy.Service.Analytics.ReadModels.ICategorySnapshotRepository;

namespace BudgetBuddy.Service.UnitTests.Handlers;

// ════════════════════════════════════════════════════════════════════════════════
// Transactions service — CategoryChangedHandler
// ════════════════════════════════════════════════════════════════════════════════
public class TransactionsCategoryChangedHandlerTests
{
    private readonly ITransactionsRepo _repo   = Substitute.For<ITransactionsRepo>();
    private readonly ILogger<TransactionsHandler> _logger = Substitute.For<ILogger<TransactionsHandler>>();

    [Fact]
    public async Task Deleted_removes_category_from_repo()
    {
        var categoryId = Guid.NewGuid();
        var evt = new CategoryChangedEvent
        {
            CategoryId = categoryId,
            UserId     = "user-1",
            Name       = "Food",
            ChangeType = CategoryChangeType.Deleted,
        };

        await TransactionsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).DeleteAsync(categoryId, CancellationToken.None);
        await _repo.DidNotReceiveWithAnyArgs().UpsertAsync(default!, default);
    }

    [Fact]
    public async Task Created_upserts_category_snapshot()
    {
        var categoryId = Guid.NewGuid();
        var evt = new CategoryChangedEvent
        {
            CategoryId = categoryId,
            UserId     = "user-1",
            Name       = "Transport",
            Icon       = "car",
            ChangeType = CategoryChangeType.Created,
        };

        await TransactionsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<BudgetBuddy.Service.Transactions.ReadModels.CategorySnapshot>(s =>
                s.CategoryId == categoryId &&
                s.UserId     == "user-1"   &&
                s.Name       == "Transport" &&
                s.Icon       == "car"),
            CancellationToken.None);
    }

    [Fact]
    public async Task Updated_upserts_category_snapshot()
    {
        var categoryId = Guid.NewGuid();
        var evt = new CategoryChangedEvent
        {
            CategoryId = categoryId,
            UserId     = "user-1",
            Name       = "Renamed",
            Icon       = "star",
            ChangeType = CategoryChangeType.Updated,
        };

        await TransactionsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<BudgetBuddy.Service.Transactions.ReadModels.CategorySnapshot>(s =>
                s.CategoryId == categoryId &&
                s.Name       == "Renamed"),
            CancellationToken.None);
    }
}

// ════════════════════════════════════════════════════════════════════════════════
// Budgets service — CategoryChangedHandler
// ════════════════════════════════════════════════════════════════════════════════
public class BudgetsCategoryChangedHandlerTests
{
    private readonly IBudgetsRepo _repo   = Substitute.For<IBudgetsRepo>();
    private readonly ILogger<BudgetsHandler> _logger = Substitute.For<ILogger<BudgetsHandler>>();

    [Fact]
    public async Task Deleted_removes_category_from_repo()
    {
        var categoryId = Guid.NewGuid();
        var evt = new CategoryChangedEvent
        {
            CategoryId = categoryId,
            UserId     = "user-1",
            Name       = "Food",
            ChangeType = CategoryChangeType.Deleted,
        };

        await BudgetsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).DeleteAsync(categoryId, CancellationToken.None);
        await _repo.DidNotReceiveWithAnyArgs().UpsertAsync(default!, default);
    }

    [Fact]
    public async Task Created_upserts_category_snapshot()
    {
        var categoryId = Guid.NewGuid();
        var evt = new CategoryChangedEvent
        {
            CategoryId = categoryId,
            UserId     = "user-1",
            Name       = "Groceries",
            Icon       = "basket",
            ChangeType = CategoryChangeType.Created,
        };

        await BudgetsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<BudgetBuddy.Service.Budgets.ReadModels.CategorySnapshot>(s =>
                s.CategoryId == categoryId &&
                s.Name       == "Groceries" &&
                s.Icon       == "basket"),
            CancellationToken.None);
    }
}

// ════════════════════════════════════════════════════════════════════════════════
// Analytics service — CategoryChangedHandler
// ════════════════════════════════════════════════════════════════════════════════
public class AnalyticsCategoryChangedHandlerTests
{
    private readonly IAnalyticsRepo _repo   = Substitute.For<IAnalyticsRepo>();
    private readonly ILogger<AnalyticsHandler> _logger = Substitute.For<ILogger<AnalyticsHandler>>();

    [Fact]
    public async Task Deleted_removes_category_from_repo()
    {
        var categoryId = Guid.NewGuid();
        var evt = new CategoryChangedEvent
        {
            CategoryId = categoryId,
            UserId     = "user-1",
            Name       = "Food",
            ChangeType = CategoryChangeType.Deleted,
        };

        await AnalyticsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).DeleteAsync(categoryId, CancellationToken.None);
        await _repo.DidNotReceiveWithAnyArgs().UpsertAsync(default!, default);
    }

    [Fact]
    public async Task Updated_upserts_category_snapshot()
    {
        var categoryId = Guid.NewGuid();
        var evt = new CategoryChangedEvent
        {
            CategoryId = categoryId,
            UserId     = "user-2",
            Name       = "Entertainment",
            Icon       = "film",
            ChangeType = CategoryChangeType.Updated,
        };

        await AnalyticsHandler.Handle(evt, _repo, _logger, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<BudgetBuddy.Service.Analytics.ReadModels.CategorySnapshot>(s =>
                s.CategoryId == categoryId &&
                s.UserId     == "user-2"   &&
                s.Name       == "Entertainment"),
            CancellationToken.None);
    }
}
