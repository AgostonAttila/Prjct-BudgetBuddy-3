using BudgetBuddy.Service.Analytics.Messaging.Handlers;
using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Messages.Events.Budgets;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Handlers;

public class AnalyticsBudgetEventsHandlerTests
{
    private readonly IBudgetReadModelRepository _repo = Substitute.For<IBudgetReadModelRepository>();

    private static readonly Guid BudgetId   = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    // ── BudgetCreatedEvent ───────────────────────────────────────────────────────

    [Fact]
    public async Task Created_upserts_budget_read_model_with_correct_fields()
    {
        var evt = new BudgetCreatedEvent
        {
            BudgetId     = BudgetId,
            UserId       = "user-1",
            CategoryId   = CategoryId,
            Amount       = 50_000m,
            CurrencyCode = "HUF",
            Year         = 2025,
            Month        = 5,
        };

        await BudgetEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<BudgetReadModel>(m =>
                m.BudgetId     == BudgetId   &&
                m.UserId       == "user-1"   &&
                m.CategoryId   == CategoryId &&
                m.Amount       == 50_000m    &&
                m.CurrencyCode == "HUF"      &&
                m.Year         == 2025       &&
                m.Month        == 5),
            CancellationToken.None);
    }

    [Fact]
    public async Task Created_does_not_call_DeleteAsync()
    {
        var evt = new BudgetCreatedEvent
        {
            BudgetId   = BudgetId,
            UserId     = "user-1",
            CategoryId = CategoryId,
            Year       = 2025,
            Month      = 5,
        };

        await BudgetEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().DeleteAsync(Guid.Empty, default);
    }

    // ── BudgetUpdatedEvent ───────────────────────────────────────────────────────

    [Fact]
    public async Task Updated_upserts_budget_read_model_with_new_amount()
    {
        var evt = new BudgetUpdatedEvent
        {
            BudgetId     = BudgetId,
            UserId       = "user-1",
            CategoryId   = CategoryId,
            Amount       = 75_000m,
            CurrencyCode = "HUF",
            Year         = 2025,
            Month        = 6,
        };

        await BudgetEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).UpsertAsync(
            Arg.Is<BudgetReadModel>(m =>
                m.BudgetId == BudgetId &&
                m.Amount   == 75_000m  &&
                m.Month    == 6),
            CancellationToken.None);
    }

    // ── BudgetDeletedEvent ───────────────────────────────────────────────────────

    [Fact]
    public async Task Deleted_calls_DeleteAsync_with_correct_id()
    {
        var evt = new BudgetDeletedEvent { BudgetId = BudgetId, UserId = "user-1" };

        await BudgetEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.Received(1).DeleteAsync(BudgetId, CancellationToken.None);
    }

    [Fact]
    public async Task Deleted_does_not_call_UpsertAsync()
    {
        var evt = new BudgetDeletedEvent { BudgetId = BudgetId, UserId = "user-1" };

        await BudgetEventsHandler.Handle(evt, _repo, CancellationToken.None);

        await _repo.DidNotReceiveWithAnyArgs().UpsertAsync(default!, default);
    }
}
